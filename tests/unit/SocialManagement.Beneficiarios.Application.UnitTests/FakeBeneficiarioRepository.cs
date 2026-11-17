using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

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
    /// <summary>Últimos parámetros recibidos por <see cref="ListarAsync"/> (para comprobar qué pidió el caso de uso).</summary>
    public BeneficiarioQueryParameters? UltimosParametros { get; private set; }

    /// <summary>
    /// Versión en memoria y simplificada: busca sin distinguir mayúsculas y usa el orden por defecto (apellidos,
    /// nombres, Id). El orden por otros campos se prueba contra SQL Server (BeneficiariosPaginacionTests).
    /// </summary>
    public Task<PagedResponse<Beneficiario>> ListarAsync(BeneficiarioQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        UltimosParametros = parameters;
        var pageNumber = parameters.Pagination.SafePageNumber;
        var pageSize = parameters.Pagination.SafePageSize;
        var term = parameters.SearchTerm?.Trim();

        var filtrados = _beneficiarios
            .Where(b => string.IsNullOrEmpty(term)
                        || b.Nombres.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || b.Apellidos.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || b.Documento.Numero.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Apellidos, StringComparer.Ordinal)
            .ThenBy(b => b.Nombres, StringComparer.Ordinal)
            .ThenBy(b => b.Id)
            .ToList();

        var items = filtrados.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedResponse<Beneficiario>.Create(items, pageNumber, pageSize, filtrados.Count));
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
