using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// <see cref="ICurrentUser"/> de la API: el usuario autenticado de la petición HTTP en curso (claim <c>name</c>
/// del token JWT). Así CreatedBy / UpdatedBy registran quién hizo cada cambio.
/// </summary>
/// <remarks>
/// IHttpContextAccessor funciona con la petición actual aunque el objeto sea singleton, por eso puede usarlo
/// el AuditableEntityInterceptor (singleton) de Infrastructure.
/// </remarks>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserName
    {
        get
        {
            var identity = httpContextAccessor.HttpContext?.User.Identity;
            return identity is { IsAuthenticated: true } ? identity.Name : null;
        }
    }
}
