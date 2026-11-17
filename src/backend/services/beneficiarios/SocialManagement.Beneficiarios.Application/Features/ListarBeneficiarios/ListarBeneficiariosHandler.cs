using AutoMapper;
using FluentValidation;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;

/// <summary>Caso de uso: listar beneficiarios con paginación, búsqueda y orden.</summary>
/// <remarks>
/// Valida los parámetros, pide la página al repositorio (que filtra, ordena y pagina en SQL Server) y convierte
/// las entidades en DTOs. El caso de uso no sabe cómo se consulta la base de datos.
/// </remarks>
public sealed class ListarBeneficiariosHandler(
    IValidator<BeneficiarioQueryParameters> validator,
    IBeneficiarioRepository repository,
    IMapper mapper)
{
    public async Task<PagedResponse<BeneficiarioDto>> HandleAsync(BeneficiarioQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        await validator.ValidateAndThrowAsync(parameters, cancellationToken);

        var page = await repository.ListarAsync(parameters, cancellationToken);

        return PagedResponse<BeneficiarioDto>.Create(
            mapper.Map<List<BeneficiarioDto>>(page.Items),
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }
}
