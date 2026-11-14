using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SocialManagement.Solicitudes.Api.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class TechnicalEndpointsTests(SolicitudesApiFactory factory) : IClassFixture<SolicitudesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_responde_Healthy_sin_consultar_dependencias()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());

        var checks = body.RootElement.GetProperty("checks").EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        Assert.Equal(["self"], checks);
    }

    [Fact]
    public async Task Version_devuelve_nombre_y_entorno_del_servicio()
    {
        var version = await _client.GetFromJsonAsync<VersionPayload>("/version", TestContext.Current.CancellationToken);

        Assert.NotNull(version);
        Assert.Equal("solicitudes-api", version.Service);
        Assert.Equal("Development", version.Environment);
        Assert.False(string.IsNullOrWhiteSpace(version.Version));
        Assert.StartsWith(".NET 10", version.Framework, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApi_publica_el_documento_en_Development()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"/version\"", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ruta_inexistente_responde_ProblemDetails()
    {
        var response = await _client.GetAsync("/no-existe", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed record VersionPayload(string Service, string Version, string Environment, string Framework);
}
