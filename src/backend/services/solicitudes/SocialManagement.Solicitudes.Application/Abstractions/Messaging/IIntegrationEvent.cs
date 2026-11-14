namespace SocialManagement.Solicitudes.Application.Abstractions.Messaging;

/// <summary>
/// Contrato base de un evento de integración: un mensaje que este microservicio
/// publica para que otros microservicios reaccionen (a través de RabbitMQ).
/// </summary>
/// <remarks>
/// No confundir con los eventos de dominio (Domain/Events), que viven dentro del servicio.
/// Fase 0: no existen eventos concretos todavía.
/// </remarks>
public interface IIntegrationEvent
{
    /// <summary>Identificador único del mensaje (permite idempotencia en el consumidor).</summary>
    Guid EventId { get; }

    /// <summary>Momento en que ocurrió el hecho, en UTC.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
