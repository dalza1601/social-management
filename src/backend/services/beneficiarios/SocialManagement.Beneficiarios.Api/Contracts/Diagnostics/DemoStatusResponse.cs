namespace SocialManagement.Beneficiarios.Api.Contracts.Diagnostics;

/// <summary>Respuesta de GET /api/demo/status.</summary>
/// <param name="ServiceName">Nombre del servicio (appsettings: ServiceInfo:Name).</param>
/// <param name="Version">Versión del ensamblado de la API.</param>
/// <param name="TimestampUtc">Fecha y hora UTC del servidor al responder.</param>
/// <param name="Status">Estado de ejecución: Running o Stopping.</param>
public sealed record DemoStatusResponse(
    string ServiceName,
    string Version,
    DateTimeOffset TimestampUtc,
    string Status);
