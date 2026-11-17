namespace SocialManagement.Beneficiarios.Application.Common.Pagination;

/// <summary>
/// Página solicitada por el cliente. Es común a todos los listados (no pertenece a una entidad).
/// </summary>
/// <param name="PageNumber">Número de página, desde 1.</param>
/// <param name="PageSize">Registros por página, de 1 a <see cref="MaxPageSize"/>.</param>
/// <remarks>
/// Dos niveles de protección:
/// - El validador del listado RECHAZA valores fuera de rango (400 con un mensaje por campo).
/// - <see cref="SafePageNumber"/> y <see cref="SafePageSize"/> los acotan por si alguien llama al repositorio sin
///   validar: la consulta a la base de datos nunca usa una página 0 ni pide un millón de filas.
/// </remarks>
public sealed record PaginationRequest(int PageNumber = 1, int PageSize = PaginationRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>Página que se usa en la consulta: 1 como mínimo.</summary>
    public int SafePageNumber => Math.Max(PageNumber, 1);

    /// <summary>Tamaño que se usa en la consulta: el valor por defecto si no es positivo, y como máximo <see cref="MaxPageSize"/>.</summary>
    public int SafePageSize => PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
}
