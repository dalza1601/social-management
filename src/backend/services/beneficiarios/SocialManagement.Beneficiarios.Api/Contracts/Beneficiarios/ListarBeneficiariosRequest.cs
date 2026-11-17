using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Application.Common.Pagination;

namespace SocialManagement.Beneficiarios.Api.Contracts.Beneficiarios;

/// <summary>
/// Parámetros de GET /api/v1/beneficiarios en la query string:
/// <c>?pageNumber=1&amp;pageSize=10&amp;searchTerm=quispe&amp;sort=-fechaRegistro</c>.
/// </summary>
/// <remarks>
/// Es plano a propósito (fácil de enviar en la URL y de documentar en OpenAPI). AutoMapper lo convierte en
/// BeneficiarioQueryParameters, que agrupa la página en un PaginationRequest (Mappings/BeneficiarioContractsProfile).
/// </remarks>
public sealed record ListarBeneficiariosRequest
{
    /// <summary>Número de página, desde 1.</summary>
    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; init; } = 1;

    /// <summary>Registros por página, de 1 a 100.</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;

    /// <summary>Texto a buscar en nombres, apellidos o número de documento (máx. 100 caracteres).</summary>
    [FromQuery(Name = "searchTerm")]
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Orden: apellidos (por defecto), nombres, numeroDocumento, fechaRegistro o estado. Con "-" delante es
    /// descendente, p. ej. "-fechaRegistro".
    /// </summary>
    [FromQuery(Name = "sort")]
    public string? Sort { get; init; }
}
