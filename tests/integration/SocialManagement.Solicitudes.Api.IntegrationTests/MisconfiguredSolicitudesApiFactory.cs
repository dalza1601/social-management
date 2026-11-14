namespace SocialManagement.Solicitudes.Api.IntegrationTests;

/// <summary>Fábrica con una configuración incompleta, para probar los mensajes de error de arranque.</summary>
public sealed class MisconfiguredSolicitudesApiFactory(Dictionary<string, string?> overrides)
    : SolicitudesApiFactory(overrides);
