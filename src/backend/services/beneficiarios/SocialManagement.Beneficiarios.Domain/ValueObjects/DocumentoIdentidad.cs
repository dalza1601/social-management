using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;

namespace SocialManagement.Beneficiarios.Domain.ValueObjects;

/// <summary>
/// Value Object: documento de identidad de una persona (tipo + número).
/// </summary>
/// <remarks>
/// Por qué es un Value Object y no dos strings sueltos:
/// - <b>Validación:</b> solo se puede crear con <see cref="Crear"/>, que valida el formato según el tipo.
///   Si existe una instancia, es válida.
/// - <b>Significado:</b> "DNI 12345678" es un concepto del dominio, no un texto cualquiera.
/// - <b>Igualdad por valor:</b> dos documentos con el mismo tipo y número son iguales (lo da el <c>record</c>).
/// - <b>Inmutabilidad:</b> las propiedades no tienen setter; para "cambiar" un documento se crea otro.
/// </remarks>
public sealed record DocumentoIdentidad
{
    private DocumentoIdentidad(TipoDocumentoIdentidad tipo, string numero)
    {
        Tipo = tipo;
        Numero = numero;
    }

    public TipoDocumentoIdentidad Tipo { get; }

    /// <summary>Número normalizado: sin espacios y en mayúsculas.</summary>
    public string Numero { get; }

    /// <summary>Crea un documento válido o lanza <see cref="DocumentoIdentidadInvalidoException"/>.</summary>
    public static DocumentoIdentidad Crear(TipoDocumentoIdentidad tipo, string? numero)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new DocumentoIdentidadInvalidoException($"El tipo de documento '{tipo}' no es válido.");
        }

        var normalizado = (numero ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizado.Length == 0)
        {
            throw new DocumentoIdentidadInvalidoException("El número de documento es obligatorio.");
        }

        var (esValido, regla) = tipo switch
        {
            TipoDocumentoIdentidad.Dni =>
                (normalizado.Length == 8 && normalizado.All(char.IsAsciiDigit),
                 "El DNI debe tener exactamente 8 dígitos."),
            TipoDocumentoIdentidad.CarnetExtranjeria =>
                (normalizado.Length is >= 9 and <= 12 && normalizado.All(char.IsAsciiLetterOrDigit),
                 "El carné de extranjería debe tener entre 9 y 12 caracteres alfanuméricos."),
            _ =>
                (normalizado.Length is >= 6 and <= 12 && normalizado.All(char.IsAsciiLetterOrDigit),
                 "El pasaporte debe tener entre 6 y 12 caracteres alfanuméricos."),
        };

        if (!esValido)
        {
            throw new DocumentoIdentidadInvalidoException(regla);
        }

        return new DocumentoIdentidad(tipo, normalizado);
    }

    public override string ToString() => $"{Tipo} {Numero}";
}
