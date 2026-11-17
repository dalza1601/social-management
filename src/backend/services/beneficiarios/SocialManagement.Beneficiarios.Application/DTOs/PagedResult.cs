namespace SocialManagement.Beneficiarios.Application.DTOs;

/// <summary>Una página de resultados y la información para navegar entre páginas.</summary>
/// <typeparam name="T">Tipo de cada elemento.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int TotalRegistros)
{
    public int TotalPaginas => TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);

    public bool TieneAnterior => Pagina > 1;

    public bool TieneSiguiente => Pagina < TotalPaginas;
}
