using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SocialManagement.Gateway.IntegrationTests;

/// <summary>Comportamiento del Gateway cuando un microservicio no está disponible.</summary>
[Trait("Category", "Integration")]
public sealed class GatewayUnavailableServiceTests
{
    // Puerto 9 (discard): no hay ningún servicio escuchando.
    private const string UnreachableAddress = "http://127.0.0.1:9/";

    [Fact]
    public async Task Responde_502_y_health_ready_503_si_el_servicio_no_responde()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseProxy:Clusters:beneficiarios:Destinations:default:Address"] = UnreachableAddress,
                ["ReverseProxy:Clusters:solicitudes:Destinations:default:Address"] = UnreachableAddress,
            }));
        });
        var client = factory.CreateClient();

        var proxied = await client.GetAsync("/api/beneficiarios/version", TestContext.Current.CancellationToken);
        var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var live = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, proxied.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
