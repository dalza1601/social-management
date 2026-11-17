using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;
using SocialManagement.Beneficiarios.Domain.Enums;

namespace SocialManagement.Beneficiarios.Api.Contracts;

/// <summary>Cuerpo de POST /api/v1/beneficiarios.</summary>
/// <param name="Nombres">Nombres del beneficiario (solo letras, espacios, apóstrofos, guiones y puntos; máx. 100).</param>
/// <param name="Apellidos">Apellidos del beneficiario (mismas reglas que los nombres).</param>
/// <param name="TipoDocumento">Dni, CarnetExtranjeria o Pasaporte.</param>
/// <param name="NumeroDocumento">Número de documento según su tipo (DNI: 8 dígitos).</param>
/// <remarks>
/// Los campos son opcionales en el contrato para que TODAS las validaciones (y sus mensajes) las haga
/// FluentValidation en el caso de uso, con un error por campo.
/// </remarks>
public sealed record RegistrarBeneficiarioRequest(
    string? Nombres,
    string? Apellidos,
    TipoDocumentoIdentidad? TipoDocumento,
    string? NumeroDocumento)
{
    public RegistrarBeneficiarioCommand ToCommand() => new(
        Nombres ?? string.Empty,
        Apellidos ?? string.Empty,
        TipoDocumento ?? default,
        NumeroDocumento ?? string.Empty);
}
