using AutoMapper;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Domain.Entities;

namespace SocialManagement.Beneficiarios.Application.Mappings;

/// <summary>
/// Mapeo de la entidad <see cref="Beneficiario"/> a su DTO de salida <see cref="BeneficiarioDto"/>.
/// Los casos de uso lo usan con <c>mapper.Map&lt;BeneficiarioDto&gt;(beneficiario)</c>.
/// </summary>
/// <remarks>
/// AutoMapper empareja por nombre: Id, Nombres, Apellidos, NombreCompleto y FechaRegistroUtc se copian solos,
/// y los enums (Estado) se convierten a texto. Solo se declara lo que NO coincide por nombre: los datos del
/// value object Documento. La prueba BeneficiarioProfileTests falla si un campo del DTO queda sin origen.
/// </remarks>
public sealed class BeneficiarioProfile : Profile
{
    public BeneficiarioProfile()
    {
        CreateMap<Beneficiario, BeneficiarioDto>()
            .ForCtorParam(nameof(BeneficiarioDto.TipoDocumento), option => option.MapFrom(beneficiario => beneficiario.Documento.Tipo.ToString()))
            .ForCtorParam(nameof(BeneficiarioDto.NumeroDocumento), option => option.MapFrom(beneficiario => beneficiario.Documento.Numero));
    }
}
