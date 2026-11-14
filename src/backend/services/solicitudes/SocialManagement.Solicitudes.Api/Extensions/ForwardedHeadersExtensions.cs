using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;
using Microsoft.Extensions.Options;
using SocialManagement.Solicitudes.Api.Configuration;

namespace SocialManagement.Solicitudes.Api.Extensions;

/// <summary>
/// Aplica X-Forwarded-For/Proto/Host/Prefix enviados por proxies de confianza (Gateway, ingress,
/// balanceador). Así Request.Scheme, Host y PathBase reflejan la URL pública, y el documento
/// OpenAPI anuncia la dirección correcta (HTTPS + dominio + /api/solicitudes).
/// </summary>
public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services)
    {
        const string section = ForwardedHeadersSettings.SectionName;

        services.AddOptions<ForwardedHeadersSettings>()
            .BindConfiguration(section)
            .Validate(
                s => s.KnownProxies.All(ip => IPAddress.TryParse(ip, out _)),
                $"'{section}:KnownProxies' contiene una dirección IP no válida.")
            .Validate(
                s => s.KnownNetworks.All(network => IPNetwork.TryParse(network, out _)),
                $"'{section}:KnownNetworks' contiene una red no válida. Usa notación CIDR, p. ej. '172.16.0.0/12'.")
            .ValidateOnStart();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                           | ForwardedHeaders.XForwardedProto
                                           | ForwardedHeaders.XForwardedHost
                                           | ForwardedHeaders.XForwardedPrefix;

                // Por defecto solo se confía en loopback; se agregan los proxies configurados.
                foreach (var proxy in settings.Value.KnownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }

                foreach (var network in settings.Value.KnownNetworks)
                {
                    options.KnownIPNetworks.Add(IPNetwork.Parse(network));
                }
            });

        return services;
    }

    /// <summary>Debe ser el primer middleware del pipeline.</summary>
    public static WebApplication UseTrustedForwardedHeaders(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value.Enabled)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }
}
