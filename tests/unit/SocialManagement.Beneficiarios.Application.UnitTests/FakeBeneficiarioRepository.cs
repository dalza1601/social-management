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
    private int _ultimoId;

    public IReadOnlyList<Beneficiario> Guardados => _beneficiarios;

    public Task<Beneficiario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_beneficiarios.SingleOrDefault(b => b.Id == id));

    public Task<bool> ExisteConDocumentoAsync(DocumentoIdentidad documento, CancellationToken cancellationToken = default) =>
        Task.FromResult(_beneficiarios.Any(b => b.Documento == documento));

    // Mismo orden que la implementación real: apellidos, nombres e Id (desempate estable).
    public Task<(IReadOnlyList<Beneficiario> Items, int Total)> ListarAsync(int pagina, int tamanoPagina, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Beneficiario> items = _beneficiarios
            .OrderBy(b => b.Apellidos, StringComparer.Ordinal)
            .ThenBy(b => b.Nombres, StringComparer.Ordinal)
            .ThenBy(b => b.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return Task.FromResult((items, _beneficiarios.Count));
    }

    public Task AgregarAsync(Beneficiario beneficiario, CancellationToken cancellationToken = default)
    {
        // Simula la columna IDENTITY de SQL Server: 1, 2, 3... En la implementación real lo hace EF Core
        // al guardar; aquí se asigna por reflexión porque el setter de Id es privado (solo para la persistencia).
        typeof(Beneficiario).GetProperty(nameof(Beneficiario.Id))!.SetValue(beneficiario, ++_ultimoId);
        _beneficiarios.Add(beneficiario);
        return Task.CompletedTask;
    }
}
