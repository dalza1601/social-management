using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// TestServer no tiene dirección IP remota. Este filtro simula desde qué IP llega la petición
/// para probar qué proxies son de confianza en los encabezados X-Forwarded-*.
/// </summary>
internal sealed class RemoteIpStartupFilter(IPAddress remoteIp) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = remoteIp;
            return nextMiddleware(context);
        });
        next(app);
    };
}
