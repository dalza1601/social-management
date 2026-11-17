using AutoMapper;
using FluentValidation;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;

/// <summary>Caso de uso: consultar un beneficiario por su identificador.</summary>
public sealed class ConsultarBeneficiarioHandler(
    IValidator<ConsultarBeneficiarioQuery> validator,
    IBeneficiarioRepository repository,
    IMapper mapper)
{
    /// <returns>El beneficiario, o <c>null</c> si no existe.</returns>
    /// <exception cref="ValidationException">El identificador no está informado.</exception>
    public async Task<BeneficiarioDto?> HandleAsync(ConsultarBeneficiarioQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var beneficiario = await repository.ObtenerPorIdAsync(query.Id, cancellationToken);

        return beneficiario is null ? null : mapper.Map<BeneficiarioDto>(beneficiario);
    }
}
