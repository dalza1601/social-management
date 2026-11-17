using SocialManagement.Beneficiarios.Application.Common.Pagination;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>Valores "seguros" de la página: el repositorio nunca consulta una página 0 ni un tamaño desmedido.</summary>
[Trait("Category", "Unit")]
public sealed class PaginationRequestTests
{
    [Theory]
    [InlineData(1, 10, 1, 10)]
    [InlineData(3, 25, 3, 25)]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(1, 0, 1, PaginationRequest.DefaultPageSize)]
    [InlineData(1, -1, 1, PaginationRequest.DefaultPageSize)]
    [InlineData(1, 500, 1, PaginationRequest.MaxPageSize)]
    public void Acota_la_pagina_y_el_tamano(int pageNumber, int pageSize, int safePageNumber, int safePageSize)
    {
        var pagination = new PaginationRequest(pageNumber, pageSize);

        Assert.Equal(safePageNumber, pagination.SafePageNumber);
        Assert.Equal(safePageSize, pagination.SafePageSize);
    }

    [Theory]
    [InlineData(0, 10, 0, false, false)]
    [InlineData(25, 10, 3, false, true)]
    [InlineData(25, 10, 3, true, false, 3)]
    public void PagedResponse_calcula_la_navegacion(int total, int pageSize, int totalPages, bool hasPrevious, bool hasNext, int pageNumber = 1)
    {
        var page = PagedResponse<int>.Create([], total, pageNumber, pageSize);

        Assert.Equal(totalPages, page.TotalPages);
        Assert.Equal(hasPrevious, page.HasPreviousPage);
        Assert.Equal(hasNext, page.HasNextPage);
    }
}
