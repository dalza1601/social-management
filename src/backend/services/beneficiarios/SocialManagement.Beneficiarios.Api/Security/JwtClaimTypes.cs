namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Nombres de los claims del token, tal como viajan en el JWT (nombres cortos estándar, sin el mapeo
/// a URIs largas de .NET: <c>JwtBearerOptions.MapInboundClaims = false</c>).
/// </summary>
public static class JwtClaimTypes
{
    /// <summary>Sujeto: identificador del usuario (RFC 7519).</summary>
    public const string Subject = "sub";

    /// <summary>Nombre del usuario (OpenID Connect). Es <c>User.Identity.Name</c> en la API.</summary>
    public const string Name = "name";

    /// <summary>Rol o roles del usuario. Es lo que evalúan <c>RequireRole</c> y <c>User.IsInRole</c>.</summary>
    public const string Role = "role";
}
