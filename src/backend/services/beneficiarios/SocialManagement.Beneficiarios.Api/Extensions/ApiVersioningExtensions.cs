using Asp.Versioning;

namespace SocialManagement.Beneficiarios.Api.Extensions;

/// <summary>
/// Versionado de las APIs por segmento de URL: <c>/api/v1/beneficiarios</c>.
/// La plantilla de ruta está en BaseApiController: <c>api/v{version:apiVersion}/[controller]</c>.
/// </summary>
/// <remarks>
/// - Un controller sin [ApiVersion] pertenece a la versión por defecto (1.0).
/// - Para publicar una versión nueva: [ApiVersion("2.0")] en el controller (o en la acción) que cambia.
/// - Las respuestas incluyen la cabecera <c>api-supported-versions</c>.
/// - Los Minimal APIs técnicos (/version, /health, /api/demo/status) no se versionan.
/// </remarks>
public static class ApiVersioningExtensions
{
    public static readonly ApiVersion DefaultVersion = new(1, 0);

    /// <returns>El builder del versionado, para encadenar la documentación OpenAPI por versión.</returns>
    public static IApiVersioningBuilder AddUrlApiVersioning(this IServiceCollection services) =>
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = DefaultVersion;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                // Grupo "v1" = documento OpenAPI "v1" (/openapi/v1.json).
                options.GroupNameFormat = "'v'VVV";
                // En OpenAPI la ruta aparece como /api/v1/... en lugar de /api/v{version}/...
                options.SubstituteApiVersionInUrl = true;
            });
}
