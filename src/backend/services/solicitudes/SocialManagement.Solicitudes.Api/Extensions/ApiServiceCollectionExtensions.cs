using System.Diagnostics;
using SocialManagement.Solicitudes.Api.Configuration;
using SocialManagement.Solicitudes.Api.Middleware;

namespace SocialManagement.Solicitudes.Api.Extensions;

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

        services.AddControllers();

        // Todas las respuestas de error usan el formato estándar RFC 9457 (application/problem+json).
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var http = context.HttpContext;
                context.ProblemDetails.Instance = $"{http.Request.Method} {http.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? http.TraceIdentifier);
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Documento OpenAPI (/openapi/v1.json) y Swagger UI (/swagger): ver ApiDocumentationExtensions.
        services.AddApiDocumentation();

        // URL pública correcta (HTTPS, dominio y prefijo) detrás del Gateway o de un ingress.
        services.AddTrustedForwardedHeaders();

        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("El proceso responde."), tags: [LivenessTag]);

        return services;
    }
}
