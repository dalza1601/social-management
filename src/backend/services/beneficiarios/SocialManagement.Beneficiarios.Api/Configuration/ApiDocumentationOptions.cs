namespace SocialManagement.Beneficiarios.Api.Configuration;

/// <summary>
/// Documentación de la API: documento OpenAPI + Swagger UI (sección <c>ApiDocumentation</c>).
/// </summary>
/// <remarks>
/// Política por ambiente (ver docs/api-documentation.md):
/// - Development: habilitada (appsettings.Development.json).
/// - Staging / QA: habilitada (appsettings.Staging.json), opcionalmente con credenciales.
/// - Production: deshabilitada. Solo puede habilitarse con <see cref="Access"/> configurado;
///   si no, el servicio no arranca.
/// </remarks>
public sealed class ApiDocumentationOptions
{
    public const string SectionName = "ApiDocumentation";

    /// <summary>Nombre del documento OpenAPI: /openapi/v1.json.</summary>
    public const string DocumentName = "v1";

    public const string OpenApiRoutePrefix = "openapi";

    public bool Enabled { get; set; }

    /// <summary>Ruta de Swagger UI, sin barras al inicio ni al final (por defecto "swagger").</summary>
    public string RoutePrefix { get; set; } = "swagger";

    /// <summary>Credenciales para proteger la documentación con autenticación HTTP Basic.</summary>
    public ApiDocumentationAccessOptions Access { get; set; } = new();

    public string OpenApiPath => $"/{OpenApiRoutePrefix}/{DocumentName}.json";
}
