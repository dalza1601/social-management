using System.Net;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.DTOs;

namespace SocialManagement.Beneficiarios.SqlServer.IntegrationTests;

/// <summary>
/// Búsqueda (searchTerm) y orden (sort) del listado ejecutados por SQL Server (LIKE y ORDER BY).
/// Cada prueba usa un apellido único inventado y lo busca, así sus totales no dependen de otras pruebas.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "SqlServer")]
public sealed class BeneficiariosBusquedaYOrdenTests(SqlServerBeneficiariosApiFactory factory) : IClassFixture<SqlServerBeneficiariosApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Apellido de 10 letras al azar (los nombres solo admiten letras): marca los registros de una prueba.</summary>
    private static string ApellidoUnico() =>
        "Z" + string.Concat(Enumerable.Range(0, 9).Select(_ => (char)('a' + Random.Shared.Next(26))));

    private async Task<HttpClient> ClienteConDatosAsync(string apellido, params string[] nombres)
    {
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);
        var client = await factory.CreateAdminClientAsync();
        foreach (var nombre in nombres)
        {
            var creado = await client.RegistrarAsync(BeneficiariosApiClient.Datos(nombre, apellido, BeneficiariosApiClient.NuevoDni()));
            Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        }

        return client;
    }

    private static async Task<PagedResponse<BeneficiarioDto>> ListarAsync(HttpClient client, string query)
    {
        var respuesta = await client.GetAsync($"{BeneficiariosApiClient.Ruta}?{query}", Ct);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.LeerAsync<PagedResponse<BeneficiarioDto>>()).Data!;
    }

    [Fact]
    public async Task Busca_por_apellidos_sin_distinguir_mayusculas_y_el_total_es_el_de_la_busqueda()
    {
        var apellido = ApellidoUnico();
        var client = await ClienteConDatosAsync(apellido, "Ana", "Luis", "Rosa");

        var pagina = await ListarAsync(client, $"searchTerm={apellido.ToLowerInvariant()}&pageSize=2");

        Assert.Equal(3, pagina.TotalCount);
        Assert.Equal(2, pagina.TotalPages);
        Assert.Equal(2, pagina.Items.Count);
        Assert.All(pagina.Items, b => Assert.Equal(apellido, b.Apellidos));
    }

    [Fact]
    public async Task Busca_por_numero_de_documento()
    {
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);
        var client = await factory.CreateAdminClientAsync();
        var dni = BeneficiariosApiClient.NuevoDni();
        Assert.Equal(HttpStatusCode.Created, (await client.RegistrarAsync(BeneficiariosApiClient.Datos("Carmen", ApellidoUnico(), dni))).StatusCode);

        var pagina = await ListarAsync(client, $"searchTerm={dni}");

        Assert.Equal(dni, Assert.Single(pagina.Items).NumeroDocumento);
    }

    [Theory]
    [InlineData("nombres", new[] { "Ana", "Luis", "Rosa" })]
    [InlineData("-nombres", new[] { "Rosa", "Luis", "Ana" })]
    [InlineData("fechaRegistro", new[] { "Luis", "Rosa", "Ana" })]
    [InlineData("-fechaRegistro", new[] { "Ana", "Rosa", "Luis" })]
    public async Task Ordena_por_el_campo_pedido(string sort, string[] esperado)
    {
        var apellido = ApellidoUnico();
        var client = await ClienteConDatosAsync(apellido, "Luis", "Rosa", "Ana");   // orden de registro

        var pagina = await ListarAsync(client, $"searchTerm={apellido}&sort={Uri.EscapeDataString(sort)}");

        Assert.Equal(esperado, pagina.Items.Select(b => b.Nombres));
    }

    [Fact]
    public async Task Un_orden_desconocido_responde_400_indicando_el_campo()
    {
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);
        var client = await factory.CreateOperadorClientAsync();

        var respuesta = await client.GetAsync($"{BeneficiariosApiClient.Ruta}?sort=edad", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var error = Assert.Single((await respuesta.LeerAsync<object>()).Errors);
        Assert.Equal("Sort", error.Field);
    }
}
