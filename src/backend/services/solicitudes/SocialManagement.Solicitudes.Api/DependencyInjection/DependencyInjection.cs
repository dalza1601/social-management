using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialManagement.Solicitudes.Api.Configuration;
using SocialManagement.Solicitudes.Api.Contracts.Common;
using SocialManagement.Solicitudes.Api.Extensions;
using SocialManagement.Solicitudes.Api.Middleware;

// Vive en la carpeta DependencyInjection/, pero su namespace es el de la capa (no "...DependencyInjection"): así Program.cs
// no necesita otro using y se evita que la clase se llame igual que su namespace (error CS0118 al usarla).
namespace SocialManagement.Solicitudes.Api;

/// <summary>
/// Registro de la capa de PRESENTACIÓN (Api), uno de los tres <c>DependencyInjection.cs</c> de Clean Architecture:
/// <code>builder.Services.AddApplication().AddInfrastructure().AddPresentation();</code>
/// Controllers, versionado, OpenAPI, ProblemDetails, manejo global de excepciones, mapeos de la Api (AutoMapper),
/// seguridad y health check básico. Los detalles largos viven en Extensions/ (un archivo por tema).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Etiqueta del health check de disponibilidad básica (el proceso responde).</summary>
    public const string LivenessTag = "live";

    /// <summary>Clave de configuración con la licencia de AutoMapper (secreto: nunca en appsettings).</summary>
    public const string AutoMapperLicenseKey = "AutoMapper:LicenseKey";

    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ServiceInfoOptions>()
            .BindConfiguration(ServiceInfoOptions.SectionName)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Name),
                $"Falta la configuración obligatoria '{ServiceInfoOptions.SectionName}:Name'.")
            .ValidateOnStart();

        // Rutas en minúsculas: [controller] de SolicitudesController → /api/v1/solicitudes.
        services.AddRouting(options => options.LowercaseUrls = true);

        // Versión en la URL de los controllers: api/v{version:apiVersion}/[controller] (ver BaseApiController),
        // con un documento OpenAPI (/openapi/v1.json) y Swagger UI (/swagger) por versión: ver ApiDocumentationExtensions.
        services.AddUrlApiVersioning()
            .AddApiDocumentation();

        // Los errores de validación del modelo (400) de los controllers usan el formato ApiResponse.
        services.AddControllers()
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

        // Mapeos de la capa Api (request → comando, carpeta Mappings); Application registra los suyos.
        // Clave de licencia: AutoMapper:LicenseKey (user-secrets o AutoMapper__LicenseKey). Sin clave, AutoMapper
        // funciona igual y escribe un aviso en el log.
        services.AddAutoMapper(
            (provider, mapper) => mapper.LicenseKey = provider.GetRequiredService<IConfiguration>()[AutoMapperLicenseKey],
            typeof(DependencyInjection).Assembly);

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
