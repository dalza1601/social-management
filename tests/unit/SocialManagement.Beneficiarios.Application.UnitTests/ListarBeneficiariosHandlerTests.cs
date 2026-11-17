using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Validators;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class ListarBeneficiariosHandlerTests
{
    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly ListarBeneficiariosHandler _handler;

    public ListarBeneficiariosHandlerTests() =>
        _handler = new ListarBeneficiariosHandler(new ListarBeneficiariosQueryValidator(), _repository);

    [Fact]
    public async Task Sin_registros_devuelve_una_pagina_vacia()
    {
        var resultado = await _handler.HandleAsync(new ListarBeneficiariosQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalRegistros);
        Assert.Equal(0, resultado.TotalPaginas);
        Assert.False(resultado.TieneAnterior);
        Assert.False(resultado.TieneSiguiente);
    }

    [Fact]
    public async Task Pagina_los_resultados_ordenados_por_apellidos_y_nombres()
    {
        await _repository.AgregarAsync("Rosa", "Vargas", "10000001");
        await _repository.AgregarAsync("Luis", "Huamán", "10000002");
        await _repository.AgregarAsync("Ana", "Quispe", "10000003");
        await _repository.AgregarAsync("Carlos", "Flores", "10000004");
        await _repository.AgregarAsync("Beatriz", "Quispe", "10000005");

        var pagina2 = await _handler.HandleAsync(new ListarBeneficiariosQuery(Pagina: 2, TamanoPagina: 2), TestContext.Current.CancellationToken);

        // Orden completo: Flores, Huamán, Quispe Ana, Quispe Beatriz, Vargas → la página 2 trae a los dos Quispe.
        Assert.Equal(["Ana Quispe", "Beatriz Quispe"], pagina2.Items.Select(b => b.NombreCompleto));
        Assert.Equal(2, pagina2.Pagina);
        Assert.Equal(2, pagina2.TamanoPagina);
        Assert.Equal(5, pagina2.TotalRegistros);
        Assert.Equal(3, pagina2.TotalPaginas);
        Assert.True(pagina2.TieneAnterior);
        Assert.True(pagina2.TieneSiguiente);
    }

    [Fact]
    public async Task La_ultima_pagina_no_tiene_siguiente()
    {
        await _repository.AgregarAsync("Ana", "Quispe", "10000001");
        await _repository.AgregarAsync("Luis", "Huamán", "10000002");
        await _repository.AgregarAsync("Rosa", "Vargas", "10000003");

        var resultado = await _handler.HandleAsync(new ListarBeneficiariosQuery(Pagina: 2, TamanoPagina: 2), TestContext.Current.CancellationToken);

        Assert.Equal("Rosa Vargas", Assert.Single(resultado.Items).NombreCompleto);
        Assert.False(resultado.TieneSiguiente);
    }

    [Theory]
    [InlineData(0, 10, "Pagina")]
    [InlineData(1, 0, "TamanoPagina")]
    [InlineData(1, 101, "TamanoPagina")]
    public async Task Rechaza_parametros_de_paginacion_fuera_de_rango(int pagina, int tamanoPagina, string campo)
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.HandleAsync(new ListarBeneficiariosQuery(pagina, tamanoPagina), TestContext.Current.CancellationToken));

        Assert.Equal(campo, Assert.Single(excepcion.Errors).PropertyName);
    }
}
