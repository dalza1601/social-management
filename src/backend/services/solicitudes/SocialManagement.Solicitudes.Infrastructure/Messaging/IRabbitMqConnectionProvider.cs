using RabbitMQ.Client;

namespace SocialManagement.Solicitudes.Infrastructure.Messaging;

/// <summary>
/// Entrega una conexión AMQP compartida (una por proceso, como recomienda RabbitMQ).
/// Los futuros publicadores y consumidores crearán sus canales a partir de esta conexión.
/// </summary>
public interface IRabbitMqConnectionProvider
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}
