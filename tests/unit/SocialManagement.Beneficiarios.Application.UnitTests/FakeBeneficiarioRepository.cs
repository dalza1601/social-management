using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>
/// Doble de prueba de <see cref="IBeneficiarioRepository"/>: guarda en una lista en memoria.
/// Los casos de uso no notan la diferencia con la implementación real (principio de sustitución).
/// </summary>
internal sealed class FakeBeneficiarioRepository : IBeneficiarioRepository
{
    private readonly List<Beneficiario> _beneficiarios = [];

    public IReadOnlyList<Beneficiario> Guardados => _beneficiarios;

    public Task<Beneficiario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_beneficiarios.SingleOrDefault(b => b.Id == id));

    public Task<bool> ExisteConDocumentoAsync(DocumentoIdentidad documento, CancellationToken cancellationToken = default) =>
        Task.FromResult(_beneficiarios.Any(b => b.Documento == documento));

    public Task AgregarAsync(Beneficiario beneficiario, CancellationToken cancellationToken = default)
    {
        _beneficiarios.Add(beneficiario);
        return Task.CompletedTask;
    }
}
