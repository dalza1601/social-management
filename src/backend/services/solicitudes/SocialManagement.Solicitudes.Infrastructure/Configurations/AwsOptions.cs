namespace SocialManagement.Solicitudes.Infrastructure.Configurations;

/// <summary>
/// Configuración de AWS (sección <c>Aws</c>).
/// Con <see cref="UseLocalStack"/> = true los clientes apuntan al emulador LocalStack
/// usando credenciales ficticias. Con false se usará AWS real y la cadena de credenciales
/// estándar del SDK (en una etapa futura; este proyecto no despliega en AWS real).
/// </summary>
public sealed class AwsOptions
{
    public const string SectionName = "Aws";

    public string Region { get; set; } = "us-east-1";

    public bool UseLocalStack { get; set; } = true;

    /// <summary>Endpoint de LocalStack, p. ej. http://localhost:4566 o http://localstack:4566.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Credencial ficticia, solo se usa con LocalStack.</summary>
    public string? AccessKey { get; set; }

    /// <summary>Credencial ficticia, solo se usa con LocalStack.</summary>
    public string? SecretKey { get; set; }

    public string DocumentsBucketName { get; set; } = "social-management-documents-dev";

    public string NotificationsQueueName { get; set; } = "social-management-notifications-dev";
}
