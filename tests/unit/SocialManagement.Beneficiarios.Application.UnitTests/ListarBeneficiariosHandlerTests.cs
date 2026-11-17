using FluentValidation;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Validators;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class ListarBeneficiariosHandlerTests
{
    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly ListarBeneficiariosHandler _handler;

    public ListarBeneficiariosHandlerTests() =>
        _handler = new ListarBeneficiariosHandler(new BeneficiarioQueryParametersValidator(), _repository, MapperDePrueba.Crear());

    private static BeneficiarioQueryParameters Parametros(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? sort = null) =>
        new(new PaginationRequest(pageNumber, pageSize), searchTerm, sort);

    private Task<PagedResponse<BeneficiarioDto>> ListarAsync(BeneficiarioQueryParameters parameters) =>
        _handler.HandleAsync(parameters, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Sin_registros_devuelve_una_pagina_vacia()
    {
        var resultado = await ListarAsync(Parametros());

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalCount);
        Assert.Equal(0, resultado.TotalPages);
        Assert.False(resultado.HasPreviousPage);
        Assert.False(resultado.HasNextPage);
    }

    [Fact]
    public async Task Pagina_los_resultados_y_los_convierte_en_DTOs()
    {
        await _repository.AgregarAsync("Rosa", "Vargas", "10000001");
        await _repository.AgregarAsync("Luis", "Huamán", "10000002");
        await _repository.AgregarAsync("Ana", "Quispe", "10000003");
        await _repository.AgregarAsync("Carlos", "Flores", "10000004");
        await _repository.AgregarAsync("Beatriz", "Quispe", "10000005");

        var pagina2 = await ListarAsync(Parametros(pageNumber: 2, pageSize: 2));

        // Orden por defecto: Flores, Huamán, Quispe Ana, Quispe Beatriz, Vargas → la página 2 trae a los dos Quispe.
        Assert.Equal(["Ana Quispe", "Beatriz Quispe"], pagina2.Items.Select(b => b.NombreCompleto));
        Assert.Equal(2, pagina2.PageNumber);
        Assert.Equal(2, pagina2.PageSize);
        Assert.Equal(5, pagina2.TotalCount);
        Assert.Equal(3, pagina2.TotalPages);
        Assert.True(pagina2.HasPreviousPage);
        Assert.True(pagina2.HasNextPage);
    }

    [Fact]
    public async Task Entrega_la_busqueda_y_el_orden_al_repositorio_y_el_total_es_el_de_la_busqueda()
    {
        await _repository.AgregarAsync("Ana", "Quispe", "10000001");
        await _repository.AgregarAsync("Luis", "Huamán", "10000002");
        await _repository.AgregarAsync("Beatriz", "Quispe", "10000003");
        var parametros = Parametros(searchTerm: "quispe", sort: "-fechaRegistro");

        var resultado = await ListarAsync(parametros);

        Assert.Same(parametros, _repository.UltimosParametros);
        Assert.Equal(2, resultado.TotalCount);
        Assert.All(resultado.Items, b => Assert.Equal("Quispe", b.Apellidos));
    }

    [Fact]
    public async Task La_ultima_pagina_no_tiene_siguiente()
    {
        await _repository.AgregarAsync("Ana", "Quispe", "10000001");
        await _repository.AgregarAsync("Luis", "Huamán", "10000002");
        await _repository.AgregarAsync("Rosa", "Vargas", "10000003");

        var resultado = await ListarAsync(Parametros(pageNumber: 2, pageSize: 2));

        Assert.Equal("Rosa Vargas", Assert.Single(resultado.Items).NombreCompleto);
        Assert.False(resultado.HasNextPage);
    }

    [Theory]
    [InlineData(0, 10, null, null, "PageNumber")]
    [InlineData(1, 0, null, null, "PageSize")]
    [InlineData(1, 101, null, null, "PageSize")]
    [InlineData(1, 10, null, "edad", "Sort")]
    [InlineData(1, 10, null, "-", "Sort")]
    public async Task Rechaza_parametros_no_validos(int pageNumber, int pageSize, string? searchTerm, string? sort, string campo)
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() => ListarAsync(Parametros(pageNumber, pageSize, searchTerm, sort)));

        Assert.Equal(campo, Assert.Single(excepcion.Errors).PropertyName);
    }

    [Fact]
    public async Task Rechaza_una_busqueda_demasiado_larga()
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() => ListarAsync(Parametros(searchTerm: new string('a', 101))));

        Assert.Equal("SearchTerm", Assert.Single(excepcion.Errors).PropertyName);
    }

    [Theory]
    [InlineData("apellidos")]
    [InlineData("-Nombres")]
    [InlineData("numeroDocumento")]
    [InlineData("-fechaRegistro")]
    [InlineData("ESTADO")]
    [InlineData("")]
    public async Task Acepta_los_campos_de_orden_permitidos_sin_distinguir_mayusculas(string sort)
    {
        var resultado = await ListarAsync(Parametros(sort: sort));

        Assert.NotNull(resultado);
    }
}
