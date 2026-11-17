using System.Net;
using SocialManagement.Beneficiarios.Application.DTOs;

namespace SocialManagement.Beneficiarios.SqlServer.IntegrationTests;

/// <summary>
/// Paginación ejecutada por SQL Server (OFFSET/FETCH) sobre una base exclusiva de esta clase,
/// para que los totales no dependan de otras pruebas.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "SqlServer")]
public sealed class BeneficiariosPaginacionTests(SqlServerBeneficiariosApiFactory factory) : IClassFixture<SqlServerBeneficiariosApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Lista_por_paginas_ordenado_por_apellidos_y_nombres()
    {
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);
        var client = await factory.CreateAdminClientAsync();

        foreach (var (nombres, apellidos) in new[] { ("Rosa", "Vargas"), ("Luis", "Huamán"), ("Beatriz", "Quispe"), ("Carlos", "Flores"), ("Ana", "Quispe") })
        {
            var creado = await client.RegistrarAsync(BeneficiariosApiClient.Datos(nombres, apellidos, BeneficiariosApiClient.NuevoDni()));
            Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        }

        var respuesta = await client.GetAsync($"{BeneficiariosApiClient.Ruta}?pagina=2&tamanoPagina=2", Ct);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pagina = (await respuesta.LeerAsync<PagedResult<BeneficiarioDto>>()).Data!;
        // Orden: Flores, Huamán, Quispe Ana, Quispe Beatriz, Vargas → la página 2 trae a los dos Quispe.
        Assert.Equal(["Ana Quispe", "Beatriz Quispe"], pagina.Items.Select(b => b.NombreCompleto));
        Assert.Equal(5, pagina.TotalRegistros);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.True(pagina.TieneAnterior);
        Assert.True(pagina.TieneSiguiente);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("pagina=abc")]
    public async Task Parametros_de_paginacion_invalidos_responden_400(string query)
    {
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);

        var client = await factory.CreateOperadorClientAsync();   // listar: lectura, permitida al Operador
        var respuesta = await client.GetAsync($"{BeneficiariosApiClient.Ruta}?{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
