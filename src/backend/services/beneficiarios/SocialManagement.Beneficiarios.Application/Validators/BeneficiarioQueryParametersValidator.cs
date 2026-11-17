using FluentValidation;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>Valida la página, la búsqueda y el orden del listado. Los errores usan los nombres de la query HTTP.</summary>
public sealed class BeneficiarioQueryParametersValidator : AbstractValidator<BeneficiarioQueryParameters>
{
    public BeneficiarioQueryParametersValidator()
    {
        RuleFor(parameters => parameters.Pagination)
            .NotNull()
            .WithMessage("La paginación es obligatoria.");

        RuleFor(parameters => parameters.Pagination.PageNumber)
            .GreaterThanOrEqualTo(1)
            .OverridePropertyName("PageNumber")
            .WithMessage("La página debe ser 1 o mayor.")
            .When(parameters => parameters.Pagination is not null);

        RuleFor(parameters => parameters.Pagination.PageSize)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize)
            .OverridePropertyName("PageSize")
            .WithMessage($"El tamaño de página debe estar entre 1 y {PaginationRequest.MaxPageSize}.")
            .When(parameters => parameters.Pagination is not null);

        RuleFor(parameters => parameters.SearchTerm)
            .MaximumLength(BeneficiarioQueryParameters.SearchTermMaxLength)
            .WithMessage($"La búsqueda no puede superar {BeneficiarioQueryParameters.SearchTermMaxLength} caracteres.");

        RuleFor(parameters => parameters.SortField)
            .Must(field => BeneficiarioQueryParameters.SortableFields.Contains(field, StringComparer.OrdinalIgnoreCase))
            .OverridePropertyName("Sort")
            .WithMessage($"El orden no es válido. Usa uno de: {string.Join(", ", BeneficiarioQueryParameters.SortableFields)}; con \"-\" delante para descendente.")
            .When(parameters => parameters.SortField is not null);
    }
}
