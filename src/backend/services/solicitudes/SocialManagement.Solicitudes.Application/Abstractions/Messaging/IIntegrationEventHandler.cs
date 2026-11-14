namespace SocialManagement.Solicitudes.Application.Abstractions.Messaging;

/// <summary>
/// Contrato para consumir (manejar) un evento de integración recibido de otro microservicio.
/// Un consumidor de RabbitMQ en Infrastructure deserializará el mensaje y delegará en este handler.
/// </summary>
/// <remarks>Fase 0: solo el contrato. No hay consumidores registrados.</remarks>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
