namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>Un usuario de demostración de <see cref="DevelopmentTokenOptions"/>.</summary>
public sealed class DevelopmentUserOptions
{
    /// <summary>Nombre de usuario (claims <c>sub</c> y <c>name</c>; también queda en CreatedBy / UpdatedBy).</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Clave de demostración (mínimo 12 caracteres). Solo en user-secrets o variables de entorno.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Roles del usuario: <see cref="Security.Roles.Administrador"/> y/o <see cref="Security.Roles.Operador"/>.</summary>
    public List<string> Roles { get; set; } = [];
}
