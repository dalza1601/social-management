namespace SocialManagement.Beneficiarios.Domain.Enums;

/// <summary>Tipos de documento de identidad aceptados para registrar a un beneficiario.</summary>
public enum TipoDocumentoIdentidad
{
    /// <summary>Documento Nacional de Identidad: 8 dígitos.</summary>
    Dni = 1,

    /// <summary>Carné de extranjería: entre 9 y 12 caracteres alfanuméricos.</summary>
    CarnetExtranjeria = 2,

    /// <summary>Pasaporte: entre 6 y 12 caracteres alfanuméricos.</summary>
    Pasaporte = 3,
}
