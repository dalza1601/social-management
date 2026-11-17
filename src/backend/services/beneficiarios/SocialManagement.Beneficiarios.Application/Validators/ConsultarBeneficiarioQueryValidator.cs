using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>Valida la consulta de un beneficiario: el identificador debe estar informado.</summary>
public sealed class ConsultarBeneficiarioQueryValidator : AbstractValidator<ConsultarBeneficiarioQuery>
{
    public ConsultarBeneficiarioQueryValidator() =>
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del beneficiario debe ser un número entero mayor que 0.");
}
