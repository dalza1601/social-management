using FluentValidation;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;

/// <summary>Caso de uso: listar beneficiarios con paginación.</summary>
public sealed class ListarBeneficiariosHandler(
    IValidator<ListarBeneficiariosQuery> validator,
    IBeneficiarioRepository repository)
{
    public async Task<PagedResult<BeneficiarioDto>> HandleAsync(ListarBeneficiariosQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var (beneficiarios, total) = await repository.ListarAsync(query.Pagina, query.TamanoPagina, cancellationToken);

        return new PagedResult<BeneficiarioDto>(
            beneficiarios.Select(BeneficiarioDto.Desde).ToList(),
            query.Pagina,
            query.TamanoPagina,
            total);
    }
}
