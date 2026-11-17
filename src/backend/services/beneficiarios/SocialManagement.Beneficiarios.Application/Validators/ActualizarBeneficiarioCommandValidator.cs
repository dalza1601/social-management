using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>Valida la actualización: identificador informado y los mismos datos personales que el registro.</summary>
public sealed class ActualizarBeneficiarioCommandValidator : AbstractValidator<ActualizarBeneficiarioCommand>
{
    public ActualizarBeneficiarioCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del beneficiario debe ser un número entero mayor que 0.");

        Include(new DatosPersonalesBeneficiarioValidator());
    }
}
