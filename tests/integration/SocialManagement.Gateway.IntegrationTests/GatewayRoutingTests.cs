using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace SocialManagement.Gateway.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class GatewayRoutingTests(GatewayFixture fixture) : IClassFixture<GatewayFixture>
{
    [Theory]
    [InlineData("beneficiarios")]
    [InlineData("solicitudes")]
    public void Existe_una_ruta_por_microservicio(string service)
    {
        var config = fixture.Factory.Services.GetRequiredService<IProxyConfigProvider>().GetConfig();

        var route = Assert.Single(config.Routes, r => r.ClusterId == service);
        Assert.Equal($"/api/{service}/{{**catch-all}}", route.Match.Path);
        Assert.Contains(config.Clusters, c => c.ClusterId == service);
    }

    [Theory]
    [InlineData("/api/beneficiarios/version", "beneficiarios", "/version")]
    [InlineData("/api/solicitudes/version", "solicitudes", "/version")]
    [InlineData("/api/beneficiarios/health", "beneficiarios", "/health")]
    [InlineData("/api/solicitudes/v1/recurso/123", "solicitudes", "/v1/recurso/123")]
    public async Task Reenvia_al_servicio_correcto_quitando_el_prefijo_publico(string gatewayPath, string expectedService, string expectedPath)
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetFromJsonAsync<FakeResponse>(gatewayPath, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(expectedService, response.Service);
        Assert.Equal(expectedPath, response.Path);
    }

    [Theory]
    [InlineData("beneficiarios")]
    [InlineData("solicitudes")]
    public async Task Informa_al_servicio_el_prefijo_host_y_protocolo_publicos(string service)
    {
        // Necesario para que Swagger UI y el documento OpenAPI del servicio usen la URL pública.
        var client = fixture.Factory.CreateClient();

        var response = await client.GetFromJsonAsync<FakeResponse>($"/api/{service}/swagger/index.html", TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal("/swagger/index.html", response.Path);
        Assert.Equal($"/api/{service}", response.ForwardedPrefix);
        Assert.Equal("localhost", response.ForwardedHost);        // host original de la petición al Gateway
        Assert.Equal("http", response.ForwardedProto);
    }

    [Fact]
    public async Task Ruta_no_configurada_responde_404()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/desconocido/version", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_del_gateway_responde_Healthy()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ready_comprueba_cada_microservicio()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var checks = body.RootElement.GetProperty("checks").EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());

        Assert.Equal("Healthy", checks["beneficiarios"]);
        Assert.Equal("Healthy", checks["solicitudes"]);
    }
}
