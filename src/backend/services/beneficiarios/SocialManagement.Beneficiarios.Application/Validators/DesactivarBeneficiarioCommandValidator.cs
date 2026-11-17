using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.DesactivarBeneficiario;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>Valida la desactivación: el identificador debe estar informado.</summary>
public sealed class DesactivarBeneficiarioCommandValidator : AbstractValidator<DesactivarBeneficiarioCommand>
{
    public DesactivarBeneficiarioCommandValidator() =>
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del beneficiario debe ser un número entero mayor que 0.");
}
