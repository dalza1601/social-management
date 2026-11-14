using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SocialManagement.Gateway.IntegrationTests;

/// <summary>
/// Levanta dos microservicios falsos y un Gateway en memoria cuyos destinos apuntan a ellos,
/// del mismo modo en que Docker Compose sobrescribe las direcciones con variables de entorno.
/// </summary>
public sealed class GatewayFixture : IAsyncLifetime
{
    private FakeDownstreamService? _beneficiarios;
    private FakeDownstreamService? _solicitudes;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _beneficiarios = await FakeDownstreamService.StartAsync("beneficiarios");
        _solicitudes = await FakeDownstreamService.StartAsync("solicitudes");

        var destinations = new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:beneficiarios:Destinations:default:Address"] = _beneficiarios.Address,
            ["ReverseProxy:Clusters:solicitudes:Destinations:default:Address"] = _solicitudes.Address,
        };

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(destinations));
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();

        if (_beneficiarios is not null)
        {
            await _beneficiarios.DisposeAsync();
        }

        if (_solicitudes is not null)
        {
            await _solicitudes.DisposeAsync();
        }
    }
}
