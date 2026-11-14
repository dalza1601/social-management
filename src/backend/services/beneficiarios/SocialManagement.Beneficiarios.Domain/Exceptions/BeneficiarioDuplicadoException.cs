using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Domain.Exceptions;

/// <summary>Ya existe un beneficiario registrado con el mismo documento de identidad.</summary>
public sealed class BeneficiarioDuplicadoException(DocumentoIdentidad documento)
    : DomainException($"Ya existe un beneficiario registrado con el documento {documento}.")
{
    public DocumentoIdentidad Documento { get; } = documento;
}
