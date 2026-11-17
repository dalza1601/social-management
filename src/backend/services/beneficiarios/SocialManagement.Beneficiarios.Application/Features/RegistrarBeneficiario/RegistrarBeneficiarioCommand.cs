using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Domain.Enums;

namespace SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

/// <summary>Datos de entrada para registrar un beneficiario (una intención de cambiar el sistema).</summary>
public sealed record RegistrarBeneficiarioCommand(
    string Nombres,
    string Apellidos,
    TipoDocumentoIdentidad TipoDocumento,
    string NumeroDocumento) : IDatosPersonalesBeneficiario;
