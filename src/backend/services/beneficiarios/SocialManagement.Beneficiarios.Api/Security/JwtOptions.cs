using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Configuración de los tokens JWT (sección <c>Jwt</c>). Emisor, audiencia y duración están en appsettings;
/// la clave de firma es un SECRETO: user-secrets (scripts/setup.ps1) o la variable de entorno <c>Jwt__SigningKey</c>.
/// </summary>
/// <remarks>
/// Se usa una clave simétrica (HMAC-SHA256): la misma clave firma y valida. Es suficiente para desarrollo y para
/// el curso; con un proveedor de identidad real (OAuth 2.0 / OpenID Connect) el servicio solo validaría tokens con
/// la clave pública del proveedor y nunca conocería la clave de firma.
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 exige una clave de al menos 256 bits (32 bytes).</summary>
    public const int MinimumSigningKeyBytes = 32;

    /// <summary>Quién emite el token (claim <c>iss</c>). La API rechaza tokens de otro emisor.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Para quién es el token (claim <c>aud</c>). La API rechaza tokens destinados a otra audiencia.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Clave secreta de firma (mínimo 32 bytes). Nunca en appsettings ni en el repositorio.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Minutos de validez de los tokens emitidos (sin refresh tokens: al expirar, se pide otro).</summary>
    public int ExpirationMinutes { get; set; } = 60;

    public bool HasValidSigningKey => Encoding.UTF8.GetByteCount(SigningKey ?? string.Empty) >= MinimumSigningKeyBytes;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
