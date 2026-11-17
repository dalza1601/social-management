using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Api.Contracts;
using SocialManagement.Beneficiarios.Api.Security;

namespace SocialManagement.Beneficiarios.Api.Endpoints;

/// <summary>
/// Minimal API SOLO PARA DESARROLLO: POST /api/auth/dev-token. Cambia las credenciales de un usuario demo
/// por un token JWT. Se registra en Program.cs únicamente si el ambiente es Development y
/// DevelopmentTokens:Enabled = true.
/// </summary>
/// <remarks>
/// No es OAuth 2.0: no hay grant_type, client_id, scopes, consentimiento ni refresh tokens. En producción los
/// tokens los emitiría un proveedor de identidad (Microsoft Entra ID, Keycloak, Auth0...) y este endpoint no existiría.
/// </remarks>
public static class IssueDevelopmentTokenEndpoint
{
    public const string Route = "/api/auth/dev-token";

    private const string LoggerCategory = "SocialManagement.Beneficiarios.Api.Security.DevelopmentTokens";

    public static Results<Ok<DevTokenResponse>, ValidationProblem, ProblemHttpResult> Handle(
        DevTokenRequest request,
        [FromServices] DevelopmentTokenIssuer issuer,
        [FromServices] ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            errors["userName"] = ["El usuario es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["La clave es obligatoria."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors, title: "La solicitud contiene datos no válidos.");
        }

        var logger = loggerFactory.CreateLogger(LoggerCategory);
        var user = issuer.FindUser(request.UserName!, request.Password!);
        if (user is null)
        {
            // Nunca se registra la clave. El mismo mensaje para usuario inexistente o clave incorrecta.
            logger.LogWarning("Credenciales de demostración no válidas para el usuario {UserName}", request.UserName);
            return TypedResults.Problem(
                title: "Credenciales no válidas.",
                detail: "El usuario o la clave no corresponden a ningún usuario de demostración.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = issuer.Issue(user);
        logger.LogInformation(
            "Token de desarrollo emitido para {UserName} con roles {Roles}, válido hasta {ExpiresAtUtc}",
            token.UserName,
            token.Roles,
            token.ExpiresAtUtc);

        return TypedResults.Ok(token);
    }
}
