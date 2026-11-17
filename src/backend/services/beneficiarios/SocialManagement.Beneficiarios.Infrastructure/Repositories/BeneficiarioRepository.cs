using Microsoft.EntityFrameworkCore;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects;
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

    public async Task<(IReadOnlyList<Beneficiario> Items, int Total)> ListarAsync(
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        var consulta = context.Beneficiarios.AsNoTracking();

        var total = await consulta.CountAsync(cancellationToken);

        // Orden total y estable (el Id desempata): las páginas no repiten ni saltan registros.
        var items = await consulta
            .OrderBy(beneficiario => beneficiario.Apellidos)
            .ThenBy(beneficiario => beneficiario.Nombres)
            .ThenBy(beneficiario => beneficiario.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AgregarAsync(Beneficiario beneficiario, CancellationToken cancellationToken = default) =>
        await context.Beneficiarios.AddAsync(beneficiario, cancellationToken);
}
