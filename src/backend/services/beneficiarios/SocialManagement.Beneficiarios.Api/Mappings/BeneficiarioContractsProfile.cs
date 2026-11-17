using AutoMapper;
using SocialManagement.Beneficiarios.Api.Contracts.Beneficiarios;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

namespace SocialManagement.Beneficiarios.Api.Mappings;

/// <summary>
/// Mapeo de los contratos HTTP de beneficiarios (Contracts/Beneficiarios) a los comandos y consultas de Application.
/// El controller lo usa con <c>Mapper.Map&lt;RegistrarBeneficiarioCommand&gt;(request)</c>.
/// </summary>
/// <remarks>
/// Los campos coinciden por nombre. Un tipo de documento ausente (null) llega como 0, que no es un valor
/// válido del enum: el validador responde "El tipo de documento no es válido.".
/// </remarks>
public sealed class BeneficiarioContractsProfile : Profile
{
    public BeneficiarioContractsProfile()
    {
        CreateMap<RegistrarBeneficiarioRequest, RegistrarBeneficiarioCommand>();

        // El Id no viaja en el cuerpo sino en la ruta (PUT /api/v1/beneficiarios/{id}): el controller lo asigna
        // con "with { Id = id }" después de mapear.
        CreateMap<ActualizarBeneficiarioRequest, ActualizarBeneficiarioCommand>()
            .ForCtorParam(nameof(ActualizarBeneficiarioCommand.Id), option => option.MapFrom(_ => 0));

        // La query plana de la URL se agrupa: pageNumber y pageSize forman el PaginationRequest. Un searchTerm o sort
        // ausente llega como "" (ContractConventionsProfile), que equivale a "sin búsqueda" y "orden por defecto".
        CreateMap<ListarBeneficiariosRequest, BeneficiarioQueryParameters>()
            .ForCtorParam(
                nameof(BeneficiarioQueryParameters.Pagination),
                option => option.MapFrom(request => new PaginationRequest(request.PageNumber, request.PageSize)));
    }
}
