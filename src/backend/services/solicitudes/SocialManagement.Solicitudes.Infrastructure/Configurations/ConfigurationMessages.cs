namespace SocialManagement.Solicitudes.Infrastructure.Configurations;

/// <summary>
/// Mensajes de error de configuración comprensibles para quien ejecuta el servicio.
/// Indican la clave faltante y cómo definirla (appsettings, variable de entorno o user-secrets).
/// </summary>
internal static class ConfigurationMessages
{
    public static string Required(string key) =>
        $"Falta la configuración obligatoria '{key}'. " +
        $"Defínela en appsettings, en user-secrets (ejecuta scripts/setup.ps1) " +
        $"o con la variable de entorno '{ToEnvironmentVariable(key)}'.";

    public static string Invalid(string key, string reason) =>
        $"La configuración '{key}' no es válida: {reason}. " +
        $"Revisa appsettings o la variable de entorno '{ToEnvironmentVariable(key)}'.";

    private static string ToEnvironmentVariable(string key) => key.Replace(":", "__", StringComparison.Ordinal);

    public static bool IsAbsoluteHttpUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
