using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SocialManagement.Beneficiarios.Api.Contracts.Authentication;

namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Comprueba las credenciales de un usuario demo y le emite un token JWT firmado (SOLO DESARROLLO).
/// </summary>
/// <remarks>
/// Contenido del token:
/// <code>
/// { "sub": "admin.demo", "name": "admin.demo", "role": ["Administrador"],
///   "iss": "social-management-dev", "aud": "social-management-api", "iat": ..., "nbf": ..., "exp": ... }
/// </code>
/// Cualquiera puede LEER un JWT (no está cifrado, solo firmado): nunca incluyas datos sensibles en los claims.
/// </remarks>
public sealed class DevelopmentTokenIssuer(
    IOptions<JwtOptions> jwtOptions,
    IOptions<DevelopmentTokenOptions> developmentOptions,
    TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler _handler = new();

    /// <summary>
    /// Devuelve el usuario demo si el nombre y la clave coinciden; si no, null. Recorre TODOS los usuarios y
    /// compara en tiempo constante, para no revelar por tiempos si el usuario existe.
    /// </summary>
    public DevelopmentUserOptions? FindUser(string userName, string password)
    {
        DevelopmentUserOptions? match = null;
        foreach (var user in developmentOptions.Value.Users)
        {
            var sameUser = SecretComparer.FixedTimeEquals(userName, user.UserName);
            var samePassword = SecretComparer.FixedTimeEquals(password, user.Password);
            if (sameUser && samePassword)
            {
                match = user;
            }
        }

        return match;
    }

    /// <summary>Emite un token para el usuario indicado, válido durante <c>Jwt:ExpirationMinutes</c>.</summary>
    public DevTokenResponse Issue(DevelopmentUserOptions user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var jwt = jwtOptions.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.ExpirationMinutes);
        var roles = user.Roles.Distinct(StringComparer.Ordinal).ToArray();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtClaimTypes.Subject] = user.UserName,
                [JwtClaimTypes.Name] = user.UserName,
                [JwtClaimTypes.Role] = roles,
            },
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new DevTokenResponse(
            AccessToken: _handler.CreateToken(descriptor),
            TokenType: "Bearer",
            ExpiresIn: jwt.ExpirationMinutes * 60,
            ExpiresAtUtc: expiresAt,
            UserName: user.UserName,
            Roles: roles);
    }
}
