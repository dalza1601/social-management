namespace SocialManagement.Beneficiarios.Application.Common.Pagination;

/// <summary>
/// Lo que RETORNA un listado paginado: los elementos de la página y la información para navegar entre páginas.
/// Es común a todos los listados: el repositorio devuelve <c>PagedResponse&lt;Beneficiario&gt;</c> y el caso de uso
/// <c>PagedResponse&lt;BeneficiarioDto&gt;</c>.
/// </summary>
/// <typeparam name="T">Tipo de cada elemento.</typeparam>
/// <param name="Items">Elementos de la página.</param>
/// <param name="PageNumber">Página devuelta, desde 1.</param>
/// <param name="PageSize">Registros por página.</param>
/// <param name="TotalCount">Total de registros de TODAS las páginas (con la búsqueda aplicada).</param>
/// <param name="TotalPages">Cantidad de páginas.</param>
/// <param name="HasPreviousPage">true si existe una página anterior.</param>
/// <param name="HasNextPage">true si existe una página siguiente.</param>
/// <remarks>Se construye con <see cref="Create"/>, que calcula los tres últimos valores.</remarks>
public sealed record PagedResponse<T>(
    IEnumerable<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    /// <summary>Crea la página a partir de sus elementos, la página pedida y el total de registros.</summary>
    public static PagedResponse<T> Create(IReadOnlyCollection<T> items, int pageNumber, int pageSize, int totalCount)
    {
        // pageSize <= 0 no debería llegar (se usa SafePageSize), pero así nunca se divide por cero.
        var totalPages = totalCount == 0 || pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasPreviousPage = pageNumber > 1;
        var hasNextPage = pageNumber < totalPages;

        return new PagedResponse<T>(items, pageNumber, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage);
    }
}
