namespace SocialManagement.Beneficiarios.Api.Contracts.Authentication;

/// <summary>Token JWT emitido para un usuario de demostración (solo Development).</summary>
/// <param name="AccessToken">Token JWT. Se envía en cada petición: <c>Authorization: Bearer {accessToken}</c>.</param>
/// <param name="TokenType">Siempre "Bearer".</param>
/// <param name="ExpiresIn">Segundos de validez desde la emisión.</param>
/// <param name="ExpiresAtUtc">Fecha y hora UTC de expiración. Después hay que pedir otro token (no hay refresh tokens).</param>
/// <param name="UserName">Usuario al que se emitió el token.</param>
/// <param name="Roles">Roles incluidos en el token.</param>
public sealed record DevTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset ExpiresAtUtc,
    string UserName,
    IReadOnlyList<string> Roles);
