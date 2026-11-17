using FluentValidation;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;

namespace SocialManagement.Beneficiarios.Application.Features.DesactivarBeneficiario;

/// <summary>
/// Caso de uso: desactivar un beneficiario. Es una baja LÓGICA: el registro se conserva con estado Inactivo
/// (historial y auditoría). Desactivar uno ya inactivo no es un error: el resultado es el mismo.
/// </summary>
public sealed class DesactivarBeneficiarioHandler(
    IValidator<DesactivarBeneficiarioCommand> validator,
    IBeneficiarioRepository repository,
    IUnitOfWork unitOfWork)
{
    /// <returns>Los datos con el estado actualizado, o <c>null</c> si el beneficiario no existe.</returns>
    public async Task<BeneficiarioDto?> HandleAsync(DesactivarBeneficiarioCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var beneficiario = await repository.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (beneficiario is null)
        {
            return null;
        }

        beneficiario.Desactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BeneficiarioDto.Desde(beneficiario);
    }
}
