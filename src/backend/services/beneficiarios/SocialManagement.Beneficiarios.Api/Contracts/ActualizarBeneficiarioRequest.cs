using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;
using SocialManagement.Beneficiarios.Domain.Enums;

namespace SocialManagement.Beneficiarios.Api.Contracts;

/// <summary>Cuerpo de PUT /api/v1/beneficiarios/{id}: los datos personales completos (reemplazo).</summary>
/// <param name="Nombres">Nombres del beneficiario.</param>
/// <param name="Apellidos">Apellidos del beneficiario.</param>
/// <param name="TipoDocumento">Dni, CarnetExtranjeria o Pasaporte.</param>
/// <param name="NumeroDocumento">Número de documento según su tipo.</param>
public sealed record ActualizarBeneficiarioRequest(
    string? Nombres,
    string? Apellidos,
    TipoDocumentoIdentidad? TipoDocumento,
    string? NumeroDocumento)
{
    public ActualizarBeneficiarioCommand ToCommand(int id) => new(
        id,
        Nombres ?? string.Empty,
        Apellidos ?? string.Empty,
        TipoDocumento ?? default,
        NumeroDocumento ?? string.Empty);
}
