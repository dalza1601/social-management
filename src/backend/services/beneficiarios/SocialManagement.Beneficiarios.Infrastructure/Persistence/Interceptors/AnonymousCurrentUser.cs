using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Implementación por defecto de <see cref="ICurrentUser"/>: sin usuario. Se usa fuera de la API
/// (herramientas, procesos en segundo plano). La API la reemplaza por el usuario del token JWT.
/// </summary>
internal sealed class AnonymousCurrentUser : ICurrentUser
{
    public string? UserName => null;
}
