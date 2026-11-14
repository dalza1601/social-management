namespace SocialManagement.Solicitudes.Api.IntegrationTests;

/// <summary>Fábrica con configuración y ambiente a elección (p. ej. Production con credenciales).</summary>
public sealed class CustomSolicitudesApiFactory(Dictionary<string, string?> overrides, string environment)
    : SolicitudesApiFactory(overrides, environment);
