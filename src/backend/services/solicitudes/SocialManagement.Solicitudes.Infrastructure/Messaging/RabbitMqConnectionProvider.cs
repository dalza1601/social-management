using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SocialManagement.Solicitudes.Infrastructure.Configurations;

namespace SocialManagement.Solicitudes.Infrastructure.Messaging;

/// <summary>
/// Crea la conexión con RabbitMQ de forma perezosa (al primer uso) y la reutiliza.
/// El servicio arranca aunque RabbitMQ no esté disponible; el estado se informa en /health/ready.
/// </summary>
internal sealed class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionProvider> logger)
    : IRabbitMqConnectionProvider, IAsyncDisposable, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                VirtualHost = settings.VirtualHost,
                UserName = settings.UserName,
                Password = settings.Password,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            };

            _connection = await factory.CreateConnectionAsync(settings.ClientProvidedName, cancellationToken);

            logger.LogInformation(
                "Conexión con RabbitMQ establecida en {Host}:{Port} (vhost {VirtualHost})",
                settings.Host,
                settings.Port,
                settings.VirtualHost);

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _lock.Dispose();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
        _lock.Dispose();
    }
}
