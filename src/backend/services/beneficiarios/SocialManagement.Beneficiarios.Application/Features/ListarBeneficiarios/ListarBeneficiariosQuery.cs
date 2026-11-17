namespace SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;

/// <summary>Consulta paginada de beneficiarios (ordenados por apellidos y nombres).</summary>
/// <param name="Pagina">Número de página, empezando en 1.</param>
/// <param name="TamanoPagina">Cantidad de registros por página (máximo <see cref="TamanoPaginaMaximo"/>).</param>
public sealed record ListarBeneficiariosQuery(int Pagina = 1, int TamanoPagina = ListarBeneficiariosQuery.TamanoPaginaPorDefecto)
{
    public const int TamanoPaginaPorDefecto = 10;
    public const int TamanoPaginaMaximo = 100;
}
