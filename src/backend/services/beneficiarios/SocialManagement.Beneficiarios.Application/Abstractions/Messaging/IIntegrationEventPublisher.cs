namespace SocialManagement.Beneficiarios.Application.Abstractions.Messaging;

/// <summary>
/// Contrato para publicar eventos de integración.
/// La capa de aplicación depende de esta interfaz; la implementación con RabbitMQ
/// vivirá en Infrastructure/Messaging (inversión de dependencias).
/// </summary>
/// <remarks>Fase 0: solo el contrato. La implementación se construye en el módulo de mensajería.</remarks>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
