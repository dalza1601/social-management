using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;
using SocialManagement.Beneficiarios.Infrastructure.Persistence;

namespace SocialManagement.Beneficiarios.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IBeneficiarioRepository"/> con EF Core y SQL Server.
/// No llama a SaveChanges: los casos de uso confirman con IUnitOfWork (el mismo DbContext de la petición).
/// </summary>
internal sealed class BeneficiarioRepository(BeneficiariosDbContext context) : IBeneficiarioRepository
{
    public Task<Beneficiario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        // Con seguimiento (tracking): si el caso de uso modifica la entidad, SaveChanges guarda los cambios.
        context.Beneficiarios.FirstOrDefaultAsync(beneficiario => beneficiario.Id == id, cancellationToken);

    public Task<bool> ExisteConDocumentoAsync(DocumentoIdentidad documento, CancellationToken cancellationToken = default) =>
        context.Beneficiarios.AnyAsync(
            beneficiario => beneficiario.Documento.Tipo == documento.Tipo
                            && beneficiario.Documento.Numero == documento.Numero,
            cancellationToken);

    public async Task<PagedResponse<Beneficiario>> ListarAsync(
        BeneficiarioQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var pageNumber = parameters.Pagination.SafePageNumber;
        var pageSize = parameters.Pagination.SafePageSize;
        var query = context.Beneficiarios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            // LIKE '%término%' en SQL Server (la intercalación por defecto no distingue mayúsculas).
            var searchTerm = parameters.SearchTerm.Trim();
            query = query.Where(beneficiario =>
                beneficiario.Nombres.Contains(searchTerm)
                || beneficiario.Apellidos.Contains(searchTerm)
                || beneficiario.Documento.Numero.Contains(searchTerm));
        }

        query = ApplySorting(query, parameters.SortField, parameters.SortDescending);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResponse<Beneficiario>.Create(items, pageNumber, pageSize, total);
    }

    public async Task AgregarAsync(Beneficiario beneficiario, CancellationToken cancellationToken = default) =>
        await context.Beneficiarios.AddAsync(beneficiario, cancellationToken);

    /// <summary>
    /// Orden pedido (el validador ya garantizó que el campo existe) y, al final, el Id como desempate: un orden total
    /// y estable hace que las páginas no repitan ni salten registros.
    /// </summary>
    private static IQueryable<Beneficiario> ApplySorting(IQueryable<Beneficiario> query, string? field, bool descending)
    {
        var ordered = field switch
        {
            "nombres" => OrderBy(query, beneficiario => beneficiario.Nombres, descending),
            "numerodocumento" => OrderBy(query, beneficiario => beneficiario.Documento.Numero, descending),
            "fecharegistro" => OrderBy(query, beneficiario => beneficiario.FechaRegistroUtc, descending),
            "estado" => OrderBy(query, beneficiario => beneficiario.Estado, descending),
            // "apellidos" o sin orden: apellidos y luego nombres.
            _ => descending
                ? query.OrderByDescending(beneficiario => beneficiario.Apellidos).ThenByDescending(beneficiario => beneficiario.Nombres)
                : query.OrderBy(beneficiario => beneficiario.Apellidos).ThenBy(beneficiario => beneficiario.Nombres),
        };

        return ordered.ThenBy(beneficiario => beneficiario.Id);
    }

    private static IOrderedQueryable<Beneficiario> OrderBy<TKey>(
        IQueryable<Beneficiario> query,
        Expression<Func<Beneficiario, TKey>> key,
        bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
