using SocialManagement.Solicitudes.Domain.Exceptions;

namespace SocialManagement.Solicitudes.Api.IntegrationTests;

/// <summary>Excepción de dominio de prueba (la API la traduce a HTTP 422).</summary>
public sealed class TestRuleViolationException(string message) : DomainException(message);
