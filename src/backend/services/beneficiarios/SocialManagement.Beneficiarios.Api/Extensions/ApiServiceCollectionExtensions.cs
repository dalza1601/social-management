using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialManagement.Beneficiarios.Api.Configuration;
using SocialManagement.Beneficiarios.Api.Contracts;
using SocialManagement.Beneficiarios.Api.Middleware;

namespace SocialManagement.Beneficiarios.Api.Extensions;

/// <summary>
/// Servicios propios de la capa de presentación: controllers, ProblemDetails,
/// manejo global de excepciones, OpenAPI y health check básico.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>Etiqueta del health check de disponibilidad básica (el proceso responde).</summary>
    public const string LivenessTag = "live";

    public static IServiceCollection AddApiPresentation(this IServiceCollection services)
    {
        services.AddOptions<ServiceInfoOptions>()
            .BindConfiguration(ServiceInfoOptions.SectionName)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Name),
                $"Falta la configuración obligatoria '{ServiceInfoOptions.SectionName}:Name'.")
            .ValidateOnStart();

        // Rutas en minúsculas: [controller] de BeneficiariosController → /api/v1/beneficiarios.
        services.AddRouting(options => options.LowercaseUrls = true);

        // Versión en la URL de los controllers: api/v{version:apiVersion}/[controller] (ver BaseApiController),
        // con un documento OpenAPI (/openapi/v1.json) y Swagger UI (/swagger) por versión: ver ApiDocumentationExtensions.
        services.AddUrlApiVersioning()
            .AddApiDocumentation();

        // Los errores de validación del modelo (400) de los controllers usan el formato ApiResponse.
        services.AddControllers()
            // Enums como texto en JSON y en OpenAPI: "tipoDocumento": "Dni" (no 1).
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value is { Errors.Count: > 0 })
                        .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError(
                            string.IsNullOrEmpty(entry.Key) ? null : entry.Key,
                            string.IsNullOrWhiteSpace(error.ErrorMessage) ? "El valor no es válido." : error.ErrorMessage)))
                        .ToList();

                    return new BadRequestObjectResult(ApiResponse<object>.Fail(
                        "La solicitud contiene datos no válidos.",
                        errors,
                        context.HttpContext.GetTraceId()));
                });

        // Minimal APIs y rutas inexistentes: errores en formato estándar RFC 9457 (application/problem+json).
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var http = context.HttpContext;
                context.ProblemDetails.Instance = $"{http.Request.Method} {http.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", http.GetTraceId());
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Reloj del sistema como abstracción (TimeProvider, incluido en .NET).
        // TryAdd: si una prueba ya registró un reloj falso, se respeta.
        services.TryAddSingleton(TimeProvider.System);

        // URL pública correcta (HTTPS, dominio y prefijo) detrás del Gateway o de un ingress.
        services.AddTrustedForwardedHeaders();

        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("El proceso responde."), tags: [LivenessTag]);

        return services;
    }
}
