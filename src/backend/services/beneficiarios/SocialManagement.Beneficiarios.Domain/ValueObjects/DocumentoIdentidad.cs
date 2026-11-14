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
    /// <summary>Longitud máxima del número para cualquier tipo de documento.</summary>
    public const int LongitudMaximaNumero = 12;

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

        var normalizado = Normalizar(numero);
        if (normalizado.Length == 0)
        {
            throw new DocumentoIdentidadInvalidoException("El número de documento es obligatorio.");
        }

        if (!EsValido(tipo, normalizado))
        {
            throw new DocumentoIdentidadInvalidoException(DescripcionDelFormato(tipo));
        }

        return new DocumentoIdentidad(tipo, normalizado);
    }

    /// <summary>
    /// ¿El número cumple el formato de su tipo? Regla única que usan <see cref="Crear"/> y los validadores
    /// de entrada (FluentValidation en Application), para no duplicarla.
    /// </summary>
    public static bool EsValido(TipoDocumentoIdentidad tipo, string? numero)
    {
        var normalizado = Normalizar(numero);
        return tipo switch
        {
            TipoDocumentoIdentidad.Dni =>
                normalizado.Length == 8 && normalizado.All(char.IsAsciiDigit),
            TipoDocumentoIdentidad.CarnetExtranjeria =>
                normalizado.Length is >= 9 and <= LongitudMaximaNumero && normalizado.All(char.IsAsciiLetterOrDigit),
            TipoDocumentoIdentidad.Pasaporte =>
                normalizado.Length is >= 6 and <= LongitudMaximaNumero && normalizado.All(char.IsAsciiLetterOrDigit),
            _ => false,
        };
    }

    /// <summary>Mensaje que describe el formato esperado para el tipo de documento.</summary>
    public static string DescripcionDelFormato(TipoDocumentoIdentidad tipo) => tipo switch
    {
        TipoDocumentoIdentidad.Dni => "El DNI debe tener exactamente 8 dígitos.",
        TipoDocumentoIdentidad.CarnetExtranjeria => "El carné de extranjería debe tener entre 9 y 12 caracteres alfanuméricos.",
        TipoDocumentoIdentidad.Pasaporte => "El pasaporte debe tener entre 6 y 12 caracteres alfanuméricos.",
        _ => $"El tipo de documento '{tipo}' no es válido.",
    };

    private static string Normalizar(string? numero) => (numero ?? string.Empty).Trim().ToUpperInvariant();

    public override string ToString() => $"{Tipo} {Numero}";
}
