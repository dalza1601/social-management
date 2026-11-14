using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SocialManagement.Gateway.Health;

/// <summary>Registro de los health checks del Gateway.</summary>
public static class GatewayHealthChecks
{
    public const string LivenessTag = "live";
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddGatewayHealthChecks(this IServiceCollection services, IConfigurationSection reverseProxySection)
    {
        var clusterIds = reverseProxySection.GetSection("Clusters").GetChildren().Select(cluster => cluster.Key).ToArray();
        if (clusterIds.Length == 0)
        {
            throw new InvalidOperationException(
                "No hay clústeres definidos en 'ReverseProxy:Clusters'. Revisa appsettings.json del Gateway.");
        }

        services.AddHttpClient(DownstreamServiceHealthCheck.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(5));

        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("El Gateway responde."), tags: [LivenessTag]);

        // Un health check por clúster (microservicio) configurado en YARP.
        foreach (var clusterId in clusterIds)
        {
            healthChecks.Add(new HealthCheckRegistration(
                clusterId,
                sp => ActivatorUtilities.CreateInstance<DownstreamServiceHealthCheck>(sp, clusterId),
                HealthStatus.Unhealthy,
                [ReadinessTag],
                TimeSpan.FromSeconds(6)));
        }

        return services;
    }
}
