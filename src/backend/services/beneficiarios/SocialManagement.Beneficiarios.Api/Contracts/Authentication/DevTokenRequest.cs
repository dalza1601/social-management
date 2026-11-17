namespace SocialManagement.Beneficiarios.Api.Contracts.Authentication;

/// <summary>Credenciales de un usuario de demostración para <c>POST /api/auth/dev-token</c> (solo Development).</summary>
/// <param name="UserName">Usuario demo configurado en DevelopmentTokens:Users (user-secrets o variables de entorno).</param>
/// <param name="Password">Clave del usuario demo.</param>
public sealed record DevTokenRequest(string? UserName, string? Password);
