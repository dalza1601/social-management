using SocialManagement.Beneficiarios.Domain.Entities;

namespace SocialManagement.Beneficiarios.Application.DTOs;

/// <summary>
/// Datos de un beneficiario que devuelven los casos de uso. Es una "foto" de solo lectura:
/// quien la recibe no puede modificar la entidad ni saltarse sus invariantes.
/// </summary>
public sealed record BeneficiarioDto(
    Guid Id,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string TipoDocumento,
    string NumeroDocumento,
    DateTimeOffset FechaRegistroUtc,
    string Estado)
{
    public static BeneficiarioDto Desde(Beneficiario beneficiario)
    {
        ArgumentNullException.ThrowIfNull(beneficiario);

        return new BeneficiarioDto(
            beneficiario.Id,
            beneficiario.Nombres,
            beneficiario.Apellidos,
            beneficiario.NombreCompleto,
            beneficiario.Documento.Tipo.ToString(),
            beneficiario.Documento.Numero,
            beneficiario.FechaRegistroUtc,
            beneficiario.Estado.ToString());
    }
}
