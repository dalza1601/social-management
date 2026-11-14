using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>Doble de prueba de <see cref="IUnitOfWork"/>: cuenta cuántas veces se confirmaron cambios.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.FromResult(1);
    }
}
