namespace SocialManagement.Beneficiarios.Infrastructure.Configurations;

/// <summary>
/// Parámetros de conexión con RabbitMQ (sección <c>RabbitMq</c>).
/// El usuario y la contraseña se proporcionan por variables de entorno o user-secrets.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Nombre visible de la conexión en la consola de administración de RabbitMQ.</summary>
    public string ClientProvidedName { get; set; } = "beneficiarios-api";
}
