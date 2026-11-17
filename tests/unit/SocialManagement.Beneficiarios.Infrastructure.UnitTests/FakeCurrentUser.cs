using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Infrastructure.UnitTests;

/// <summary>Usuario actual controlable desde la prueba (en la API sería el usuario del token JWT).</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserName { get; set; }
}
