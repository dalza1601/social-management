using Microsoft.Extensions.Diagnostics.HealthChecks;
using Yarp.ReverseProxy.Configuration;

namespace SocialManagement.Gateway.Health;

/// <summary>
/// Comprueba que el Gateway alcanza el endpoint /health del microservicio de un clúster YARP.
/// La dirección se toma de la configuración efectiva de YARP, por lo que siempre coincide
/// con el destino real al que se enrutan las peticiones.
/// </summary>
internal sealed class DownstreamServiceHealthCheck(
    string clusterId,
    IProxyConfigProvider proxyConfigProvider,
    IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string HttpClientName = "downstream-health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cluster = proxyConfigProvider.GetConfig().Clusters.FirstOrDefault(c => c.ClusterId == clusterId);
        var address = cluster?.Destinations?.Values.FirstOrDefault()?.Address;

        if (!Uri.TryCreate(address, UriKind.Absolute, out var baseAddress))
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"El clúster '{clusterId}' no tiene una dirección de destino válida.");
        }

        var healthUri = new Uri(baseAddress, "health");
        var data = new Dictionary<string, object> { ["destination"] = healthUri.ToString() };

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(healthUri, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{healthUri} respondió {(int)response.StatusCode}.", data)
                : new HealthCheckResult(context.Registration.FailureStatus, $"{healthUri} respondió {(int)response.StatusCode}.", data: data);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"No fue posible contactar {healthUri}.",
                exception,
                data);
        }
    }
}
