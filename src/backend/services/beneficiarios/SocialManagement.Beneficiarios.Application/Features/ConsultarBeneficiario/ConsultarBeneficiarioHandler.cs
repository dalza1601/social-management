using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;

/// <summary>Caso de uso: consultar un beneficiario por su identificador.</summary>
public sealed class ConsultarBeneficiarioHandler(IBeneficiarioRepository repository)
{
    /// <returns>El beneficiario, o <c>null</c> si no existe.</returns>
    public async Task<BeneficiarioDto?> HandleAsync(ConsultarBeneficiarioQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var beneficiario = await repository.ObtenerPorIdAsync(query.Id, cancellationToken);

        return beneficiario is null ? null : BeneficiarioDto.Desde(beneficiario);
    }
}
