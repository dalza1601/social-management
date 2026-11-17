namespace SocialManagement.Beneficiarios.Application.DTOs;

/// <summary>
/// Datos de un beneficiario que devuelven los casos de uso. Es una "foto" de solo lectura:
/// quien la recibe no puede modificar la entidad ni saltarse sus invariantes.
/// </summary>
/// <remarks>Se arma desde la entidad con AutoMapper: ver Mappings/BeneficiarioProfile.cs.</remarks>
public sealed record BeneficiarioDto(
    int Id,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string TipoDocumento,
    string NumeroDocumento,
    DateTimeOffset FechaRegistroUtc,
    string Estado);
