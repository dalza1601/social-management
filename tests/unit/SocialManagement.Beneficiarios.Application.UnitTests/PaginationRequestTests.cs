using SocialManagement.Beneficiarios.Application.Common.Pagination;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>
/// Tipos de datos del paginado: <see cref="PaginationRequest"/> (lo que recibe) y <see cref="PagedResponse{T}"/>
/// (lo que retorna).
/// </summary>
[Trait("Category", "Unit")]
public sealed class PaginationRequestTests
{
    [Fact]
    public void Por_defecto_pide_la_primera_pagina_de_20_registros()
    {
        var pagination = new PaginationRequest();

        Assert.Equal(1, pagination.PageNumber);
        Assert.Equal(20, pagination.PageSize);
        Assert.Equal(100, PaginationRequest.MaxPageSize);
    }

    [Theory]
    [InlineData(1, 10, 1, 10)]
    [InlineData(3, 25, 3, 25)]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(1, 0, 1, 20)]
    [InlineData(1, -1, 1, 20)]
    [InlineData(1, 100, 1, 100)]
    [InlineData(1, 500, 1, 100)]
    public void Los_valores_seguros_acotan_la_pagina_y_el_tamano(int pageNumber, int pageSize, int safePageNumber, int safePageSize)
    {
        var pagination = new PaginationRequest(pageNumber, pageSize);

        Assert.Equal(safePageNumber, pagination.SafePageNumber);
        Assert.Equal(safePageSize, pagination.SafePageSize);
    }

    [Theory]
    [InlineData(1, 10, 0, 0, false, false)]
    [InlineData(1, 10, 25, 3, false, true)]
    [InlineData(2, 10, 25, 3, true, true)]
    [InlineData(3, 10, 25, 3, true, false)]
    [InlineData(1, 20, 20, 1, false, false)]
    [InlineData(1, 0, 5, 0, false, false)]
    public void Create_calcula_el_total_de_paginas_y_la_navegacion(
        int pageNumber, int pageSize, int totalCount, int totalPages, bool hasPreviousPage, bool hasNextPage)
    {
        var page = PagedResponse<int>.Create([], pageNumber, pageSize, totalCount);

        Assert.Equal(new PagedResponse<int>(page.Items, pageNumber, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage), page);
    }

    [Fact]
    public void Create_conserva_los_elementos_de_la_pagina()
    {
        int[] items = [7, 8, 9];

        var page = PagedResponse<int>.Create(items, pageNumber: 1, pageSize: 3, totalCount: 9);

        Assert.Equal(items, page.Items);
        Assert.Equal(3, page.TotalPages);
    }
}
