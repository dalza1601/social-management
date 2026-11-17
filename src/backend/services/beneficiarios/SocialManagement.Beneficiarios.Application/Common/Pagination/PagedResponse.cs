namespace SocialManagement.Beneficiarios.Application.Common.Pagination;

/// <summary>
/// Una página de resultados y la información para navegar entre páginas. Es común a todos los listados:
/// el repositorio devuelve <c>PagedResponse&lt;Beneficiario&gt;</c> y el caso de uso <c>PagedResponse&lt;BeneficiarioDto&gt;</c>.
/// </summary>
/// <typeparam name="T">Tipo de cada elemento.</typeparam>
/// <remarks>Se construye con <see cref="Create"/>.</remarks>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Crea la página con sus elementos, el total de registros (de todas las páginas) y la página pedida.</summary>
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize) =>
        new(items, totalCount, pageNumber, pageSize);
}
