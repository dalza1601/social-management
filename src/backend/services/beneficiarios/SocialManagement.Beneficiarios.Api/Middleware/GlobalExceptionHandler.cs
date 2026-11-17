using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Api.Contracts;
using SocialManagement.Beneficiarios.Api.Controllers;
using SocialManagement.Beneficiarios.Api.Extensions;
using SocialManagement.Beneficiarios.Domain.Exceptions;

namespace SocialManagement.Beneficiarios.Api.Middleware;

/// <summary>
/// Manejo centralizado de excepciones: las registra en el log y responde con el código HTTP adecuado.
/// Ningún endpoint necesita try/catch.
/// </summary>
/// <remarks>
/// | Excepción                              | HTTP | Contenido                         |
/// | ValidationException (FluentValidation) | 400  | Errores por campo                 |
/// | DomainConflictException                | 409  | Mensaje del conflicto             |
/// | DomainException                        | 422  | Mensaje de la regla incumplida    |
/// | Cualquier otra                         | 500  | Sin detalles internos             |
/// Formato según el origen:
/// - Controllers que heredan de <see cref="BaseApiController"/>: <see cref="ApiResponse{T}"/>.
/// - Resto (Minimal APIs, rutas inexistentes): ProblemDetails (RFC 9457).
/// </remarks>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string ValidationTitle = "La solicitud contiene datos no válidos.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, ValidationTitle),
            DomainConflictException => (StatusCodes.Status409Conflict, "Conflicto con un registro existente"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regla de dominio incumplida"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor"),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Excepción no controlada al procesar {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Solicitud rechazada {Method} {Path}: {Message}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        var errors = ToApiErrors(exception, statusCode);
        httpContext.Response.StatusCode = statusCode;

        if (httpContext.UsesApiResponseFormat())
        {
            await httpContext.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(title, errors, httpContext.GetTraceId()),
                cancellationToken);
            return true;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = ToProblemDetails(exception, statusCode, title),
        });
    }

    private static IReadOnlyList<ApiError> ToApiErrors(Exception exception, int statusCode) => exception switch
    {
        ValidationException validation => validation.Errors
            .Select(failure => new ApiError(failure.PropertyName, failure.ErrorMessage))
            .ToList(),
        // Nunca se exponen detalles internos de un error 500 al cliente.
        _ when statusCode >= StatusCodes.Status500InternalServerError => [],
        _ => [new ApiError(null, exception.Message)],
    };

    private static ProblemDetails ToProblemDetails(Exception exception, int statusCode, string title) => exception switch
    {
        // Formato estándar de errores de validación: { "errors": { "Campo": ["mensaje", ...] } }
        ValidationException validation => new HttpValidationProblemDetails(
            validation.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()))
        {
            Status = statusCode,
            Title = title,
        },
        _ => new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode < StatusCodes.Status500InternalServerError ? exception.Message : null,
        },
    };
}
