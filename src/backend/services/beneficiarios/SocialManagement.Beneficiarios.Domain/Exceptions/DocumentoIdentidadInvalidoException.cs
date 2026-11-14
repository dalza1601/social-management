namespace SocialManagement.Beneficiarios.Domain.Exceptions;

/// <summary>El documento de identidad no cumple el formato de su tipo.</summary>
public sealed class DocumentoIdentidadInvalidoException(string message) : DomainException(message);
