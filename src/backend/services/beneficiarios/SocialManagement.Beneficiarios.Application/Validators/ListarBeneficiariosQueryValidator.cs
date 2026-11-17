using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>Valida los parámetros de paginación.</summary>
public sealed class ListarBeneficiariosQueryValidator : AbstractValidator<ListarBeneficiariosQuery>
{
    public ListarBeneficiariosQueryValidator()
    {
        RuleFor(query => query.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser 1 o mayor.");

        RuleFor(query => query.TamanoPagina)
            .InclusiveBetween(1, ListarBeneficiariosQuery.TamanoPaginaMaximo)
            .WithMessage($"El tamaño de página debe estar entre 1 y {ListarBeneficiariosQuery.TamanoPaginaMaximo}.");
    }
}
