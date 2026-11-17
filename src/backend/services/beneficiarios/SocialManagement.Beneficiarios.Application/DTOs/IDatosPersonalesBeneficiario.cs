using SocialManagement.Beneficiarios.Domain.Enums;

namespace SocialManagement.Beneficiarios.Application.DTOs;

/// <summary>
/// Datos personales que comparten los comandos de registrar y actualizar un beneficiario.
/// Permite validarlos con un único validador (DatosPersonalesBeneficiarioValidator).
/// </summary>
public interface IDatosPersonalesBeneficiario
{
    string Nombres { get; }

    string Apellidos { get; }

    TipoDocumentoIdentidad TipoDocumento { get; }

    string NumeroDocumento { get; }
}
