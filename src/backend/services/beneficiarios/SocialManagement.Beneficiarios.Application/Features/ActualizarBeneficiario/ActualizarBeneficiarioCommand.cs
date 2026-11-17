using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Domain.Enums;

namespace SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;

/// <summary>Datos de entrada para corregir los datos personales de un beneficiario.</summary>
public sealed record ActualizarBeneficiarioCommand(
    int Id,
    string Nombres,
    string Apellidos,
    TipoDocumentoIdentidad TipoDocumento,
    string NumeroDocumento) : IDatosPersonalesBeneficiario;
