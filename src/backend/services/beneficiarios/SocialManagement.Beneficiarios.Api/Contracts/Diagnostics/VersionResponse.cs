namespace SocialManagement.Beneficiarios.Api.Contracts.Diagnostics;

/// <summary>Respuesta del endpoint /version.</summary>
public sealed record VersionResponse(
    string Service,
    string Version,
    string Environment,
    string Framework,
    DateTimeOffset TimestampUtc);
