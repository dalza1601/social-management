using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SocialManagement.Beneficiarios.Api.Security;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Tokens JWT para las pruebas, firmados con la misma clave de prueba que recibe la API en
/// <see cref="BeneficiariosApiFactory.ValidSettings"/>. Permite fabricar también tokens inválidos
/// (expirados, de otro emisor, firmados con otra clave...) para comprobar que la API los rechaza.
/// </summary>
internal static class TestTokens
{
    /// <summary>Clave de firma EXCLUSIVA de las pruebas (40 bytes).</summary>
    public const string SigningKey = "Clave-De-Firma-Solo-Para-Pruebas-0123456";

    // Emisor y audiencia: los de appsettings.json (no son secretos).
    public const string Issuer = "social-management-dev";
    public const string Audience = "social-management-api";

    // Usuarios demo que la fábrica configura (como harían user-secrets o variables de entorno).
    public const string AdminUser = "admin.prueba";
    public const string AdminPassword = "Clave-Admin-De-Prueba";
    public const string OperadorUser = "operador.prueba";
    public const string OperadorPassword = "Clave-Operador-De-Prueba";

    public static string Administrador() => Crear(AdminUser, [Roles.Administrador]);

    public static string Operador() => Crear(OperadorUser, [Roles.Operador]);

    /// <summary>Crea un token firmado. Los parámetros opcionales permiten romper una sola cosa cada vez.</summary>
    public static string Crear(
        string userName,
        IReadOnlyCollection<string> roles,
        DateTimeOffset? expiresAt = null,
        string issuer = Issuer,
        string audience = Audience,
        string signingKey = SigningKey)
    {
        var expires = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30);
        var claims = new Dictionary<string, object> { ["sub"] = userName, ["name"] = userName };
        if (roles.Count > 0)
        {
            claims["role"] = roles.ToArray();
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expires.AddHours(-1).UtcDateTime,
            NotBefore = expires.AddHours(-1).UtcDateTime,
            Expires = expires.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }

    public static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);
}
