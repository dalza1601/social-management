namespace SocialManagement.Beneficiarios.Application.Common.Pagination;

/// <summary>
/// Lo que RECIBE un listado paginado: la página pedida por el cliente. Es común a todos los listados.
/// </summary>
/// <param name="PageNumber">Número de página, desde 1.</param>
/// <param name="PageSize">Registros por página, de 1 a <see cref="MaxPageSize"/> (por defecto <see cref="DefaultPageSize"/>).</param>
/// <remarks>
/// El repositorio usa <see cref="SafePageNumber"/> y <see cref="SafePageSize"/>, nunca los valores tal cual: aunque
/// alguien lo llame sin validar, la consulta no usa una página 0 ni pide más de <see cref="MaxPageSize"/> filas.
/// (El validador del listado, además, responde 400 si los valores están fuera de rango.)
/// </remarks>
public sealed record PaginationRequest(int PageNumber = 1, int PageSize = PaginationRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    /// <summary>Página que se consulta: 1 si se pidió una menor.</summary>
    public int SafePageNumber => PageNumber < 1 ? 1 : PageNumber;

    /// <summary>Tamaño que se consulta, siempre dentro del rango válido.</summary>
    public int SafePageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };
}
