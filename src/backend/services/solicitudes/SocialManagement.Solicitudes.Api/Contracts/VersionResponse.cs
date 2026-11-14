namespace SocialManagement.Solicitudes.Api.Contracts;

/// <summary>Respuesta del endpoint /version.</summary>
public sealed record VersionResponse(
    string Service,
    string Version,
    string Environment,
    string Framework,
    DateTimeOffset TimestampUtc);
