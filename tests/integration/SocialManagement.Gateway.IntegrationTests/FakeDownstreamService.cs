using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SocialManagement.Gateway.IntegrationTests;

/// <summary>
/// Microservicio falso: responde con su nombre y la ruta exacta que recibió.
/// Permite comprobar a dónde y con qué ruta reenvía el Gateway.
/// </summary>
internal sealed class FakeDownstreamService : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FakeDownstreamService(WebApplication app, string address)
    {
        _app = app;
        Address = address;
    }

    /// <summary>Dirección base con barra final, p. ej. http://127.0.0.1:51234/</summary>
    public string Address { get; }

    public static async Task<FakeDownstreamService> StartAsync(string name)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.Run(context => context.Response.WriteAsJsonAsync(new FakeResponse(
            name,
            context.Request.Path.Value ?? "/",
            context.Request.Headers["X-Forwarded-Prefix"].ToString(),
            context.Request.Headers["X-Forwarded-Host"].ToString(),
            context.Request.Headers["X-Forwarded-Proto"].ToString())));

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        return new FakeDownstreamService(app, address.TrimEnd('/') + "/");
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
