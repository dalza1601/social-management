namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Emisión de tokens SOLO PARA DESARROLLO (sección <c>DevelopmentTokens</c>): <c>POST /api/auth/dev-token</c>.
/// </summary>
/// <remarks>
/// - <see cref="Enabled"/> se activa en appsettings.Development.json. Fuera de Development la API no arranca
///   si está activado, y el endpoint nunca se publica.
/// - Los usuarios demo (nombre, clave y roles) NO están en el código ni en appsettings: llegan por user-secrets
///   (scripts/setup.ps1 los copia de .env) o por variables de entorno, p. ej.
///   <c>DevelopmentTokens__Users__0__UserName</c>, <c>DevelopmentTokens__Users__0__Password</c>,
///   <c>DevelopmentTokens__Users__0__Roles__0</c>.
/// - No es un proveedor de identidad: no hay registro de usuarios, refresh tokens ni revocación.
/// </remarks>
public sealed class DevelopmentTokenOptions
{
    public const string SectionName = "DevelopmentTokens";

    public const int MinimumPasswordLength = 12;

    /// <summary>Publica el endpoint de emisión de tokens. Solo se permite en el ambiente Development.</summary>
    public bool Enabled { get; set; }

    /// <summary>Usuarios de demostración que pueden pedir un token.</summary>
    public List<DevelopmentUserOptions> Users { get; set; } = [];
}
