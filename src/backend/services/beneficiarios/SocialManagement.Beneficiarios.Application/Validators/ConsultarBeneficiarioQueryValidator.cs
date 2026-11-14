using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>Valida la consulta de un beneficiario: el identificador debe estar informado.</summary>
public sealed class ConsultarBeneficiarioQueryValidator : AbstractValidator<ConsultarBeneficiarioQuery>
{
    public ConsultarBeneficiarioQueryValidator() =>
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del beneficiario es obligatorio.");
}
