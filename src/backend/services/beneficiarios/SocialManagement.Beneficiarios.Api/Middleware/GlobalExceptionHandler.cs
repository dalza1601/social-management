using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Domain.Exceptions;

namespace SocialManagement.Beneficiarios.Api.Middleware;

/// <summary>
/// Manejo centralizado de excepciones. Convierte cualquier excepción no controlada en una
/// respuesta ProblemDetails y la registra en el log. Los controllers no necesitan try/catch.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
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

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // Nunca se exponen detalles internos de un error 500 al cliente.
                Detail = statusCode < StatusCodes.Status500InternalServerError ? exception.Message : null,
            },
        });
    }
}
