namespace SocialManagement.Beneficiarios.Api.Configuration;

/// <summary>
/// Control de acceso a la documentación (sección <c>ApiDocumentation:Access</c>). Si se definen
/// usuario y contraseña, Swagger UI y el documento OpenAPI exigen autenticación HTTP Basic.
/// Los valores se proporcionan por variables de entorno o un gestor de secretos, NUNCA en appsettings.json.
/// </summary>
public sealed class ApiDocumentationAccessOptions
{
    public const int MinimumPasswordLength = 12;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);
}
