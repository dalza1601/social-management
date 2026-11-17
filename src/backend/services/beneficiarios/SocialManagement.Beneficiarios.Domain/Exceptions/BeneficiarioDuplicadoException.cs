using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Domain.Exceptions;

/// <summary>Ya existe un beneficiario registrado con el mismo documento de identidad (HTTP 409).</summary>
public sealed class BeneficiarioDuplicadoException(DocumentoIdentidad documento)
    : DomainConflictException($"Ya existe un beneficiario registrado con el documento {documento}.")
{
    public DocumentoIdentidad Documento { get; } = documento;
}
