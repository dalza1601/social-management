namespace SocialManagement.Gateway.IntegrationTests;

/// <summary>Respuesta del microservicio falso: qué ruta y qué encabezados (X-Forwarded-*, Authorization) recibió.</summary>
internal sealed record FakeResponse(
    string Service,
    string Path,
    string ForwardedPrefix = "",
    string ForwardedHost = "",
    string ForwardedProto = "",
    string Authorization = "");
