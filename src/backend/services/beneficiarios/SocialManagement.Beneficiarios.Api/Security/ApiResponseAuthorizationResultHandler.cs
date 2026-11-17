using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SocialManagement.Beneficiarios.Api.Contracts.Common;
using SocialManagement.Beneficiarios.Api.Extensions;

namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Respuestas 401 y 403 de los controllers en el formato estándar <see cref="ApiResponse{T}"/>.
/// </summary>
/// <remarks>
/// Primero actúa el comportamiento por defecto de ASP.NET Core (JwtBearer fija el código y la cabecera
/// <c>WWW-Authenticate: Bearer error="invalid_token"...</c>); después se escribe el cuerpo JSON.
/// - 401 Unauthorized: no hay token o no es válido (firma, emisor, audiencia o expiración). "¿Quién eres?"
/// - 403 Forbidden: el token es válido, pero el usuario no tiene el rol que exige la política. "Sé quién eres, pero no puedes."
/// </remarks>
internal sealed class ApiResponseAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public const string UnauthorizedMessage =
        "Se requiere autenticación: envía un token JWT válido en la cabecera Authorization (Bearer).";

    public const string ForbiddenMessage = "No tienes permisos para realizar esta operación.";

    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        await _default.HandleAsync(next, context, policy, authorizeResult);

        if (authorizeResult.Succeeded || context.Response.HasStarted || !context.UsesApiResponseFormat())
        {
            return;
        }

        var message = authorizeResult.Forbidden ? ForbiddenMessage : UnauthorizedMessage;
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(message, traceId: context.GetTraceId()),
            context.RequestAborted);
    }
}
