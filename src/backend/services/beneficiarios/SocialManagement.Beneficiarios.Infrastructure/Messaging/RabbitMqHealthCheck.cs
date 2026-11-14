using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SocialManagement.Beneficiarios.Infrastructure.Messaging;

/// <summary>
/// Comprueba que el servicio puede abrir (o mantiene abierta) la conexión AMQP con RabbitMQ.
/// </summary>
public sealed class RabbitMqHealthCheck(IRabbitMqConnectionProvider connectionProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy("Conexión AMQP abierta.")
                : new HealthCheckResult(context.Registration.FailureStatus, "La conexión AMQP está cerrada.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "No fue posible conectar con RabbitMQ.",
                exception);
        }
    }
}
