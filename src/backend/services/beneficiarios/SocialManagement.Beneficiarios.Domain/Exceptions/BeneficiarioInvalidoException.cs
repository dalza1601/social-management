namespace SocialManagement.Beneficiarios.Domain.Exceptions;

/// <summary>Los datos para crear o modificar un beneficiario incumplen una invariante del dominio.</summary>
public sealed class BeneficiarioInvalidoException(string message) : DomainException(message);
