using SocialManagement.Beneficiarios.Application.Common.Pagination;

namespace SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;

/// <summary>Parámetros del listado de beneficiarios: página, búsqueda y orden.</summary>
/// <param name="Pagination">Página solicitada (número y tamaño).</param>
/// <param name="SearchTerm">Texto a buscar en nombres, apellidos o número de documento (opcional).</param>
/// <param name="Sort">
/// Campo de orden (opcional): uno de <see cref="SortableFields"/>; con "-" delante es descendente (p. ej. "-fechaRegistro").
/// Por defecto: apellidos y nombres, ascendente.
/// </param>
public sealed record BeneficiarioQueryParameters(
    PaginationRequest Pagination,
    string? SearchTerm = null,
    string? Sort = null)
{
    public const int SearchTermMaxLength = 100;

    /// <summary>Campos por los que se puede ordenar (sin distinguir mayúsculas).</summary>
    public static IReadOnlyList<string> SortableFields { get; } = ["apellidos", "nombres", "numeroDocumento", "fechaRegistro", "estado"];

    /// <summary>Campo de orden normalizado (minúsculas, sin "-"); null si no se pidió un orden.</summary>
    public string? SortField => string.IsNullOrWhiteSpace(Sort) ? null : Sort.Trim().TrimStart('-').ToLowerInvariant();

    /// <summary>true si el orden es descendente ("-" delante del campo).</summary>
    public bool SortDescending => Sort?.Trim().StartsWith('-') == true;
}
