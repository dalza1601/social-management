using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Prueba del endpoint GET /api/demo/status a través de todo el pipeline HTTP
/// (routing, controller, serialización JSON y OpenAPI), con un reloj falso.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DemoStatusEndpointTests : IClassFixture<BeneficiariosApiFactory>
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

    private readonly WebApplicationFactory<Program> _factory;

    public DemoStatusEndpointTests(BeneficiariosApiFactory factory)
    {
        // ConfigureTestServices se aplica DESPUÉS de Program.cs: reemplaza el reloj del sistema.
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(FixedNow))));
    }

    [Fact]
    public async Task Get_status_responde_200_con_el_estado_del_servicio()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<DemoStatusPayload>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("beneficiarios-api", body.ServiceName);
        Assert.False(string.IsNullOrWhiteSpace(body.Version));
        Assert.Equal(FixedNow, body.TimestampUtc);
        Assert.Equal("Running", body.Status);
    }

    [Fact]
    public async Task El_JSON_usa_camelCase()
    {
        var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/api/demo/status", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(["serviceName", "version", "timestampUtc", "status"], properties);
    }

    [Fact]
    public async Task OpenApi_documenta_el_endpoint_con_sus_respuestas()
    {
        var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("SocialManagement · beneficiarios-api", root.GetProperty("info").GetProperty("title").GetString());

        var operation = root.GetProperty("paths").GetProperty("/api/demo/status").GetProperty("get");
        Assert.Equal("GetDemoStatus", operation.GetProperty("operationId").GetString());
        Assert.Equal("Estado de ejecución del servicio (demostración)", operation.GetProperty("summary").GetString());
        Assert.Equal("Demo", operation.GetProperty("tags")[0].GetString());

        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _), "Falta la respuesta 200 en OpenAPI.");
        Assert.True(responses.TryGetProperty("503", out _), "Falta la respuesta 503 en OpenAPI.");
    }

    private sealed record DemoStatusPayload(string ServiceName, string Version, DateTimeOffset TimestampUtc, string Status);
}
