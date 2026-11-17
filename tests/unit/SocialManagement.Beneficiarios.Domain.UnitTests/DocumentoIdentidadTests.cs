using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

namespace SocialManagement.Beneficiarios.Domain.UnitTests;

/// <summary>Invariantes del Value Object DocumentoIdentidad. Todos los números son ficticios.</summary>
[Trait("Category", "Unit")]
public sealed class DocumentoIdentidadTests
{
    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "12345678", "12345678")]
    [InlineData(TipoDocumentoIdentidad.Dni, "  12345678 ", "12345678")]
    [InlineData(TipoDocumentoIdentidad.CarnetExtranjeria, "001234567", "001234567")]
    [InlineData(TipoDocumentoIdentidad.CarnetExtranjeria, "ce1234567890", "CE1234567890")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, " ab123456 ", "AB123456")]
    public void Crea_un_documento_valido_y_normaliza_el_numero(TipoDocumentoIdentidad tipo, string numero, string esperado)
    {
        var documento = DocumentoIdentidad.Crear(tipo, numero);

        Assert.Equal(tipo, documento.Tipo);
        Assert.Equal(esperado, documento.Numero);
    }

    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "1234567", "8 dígitos")]           // corto
    [InlineData(TipoDocumentoIdentidad.Dni, "123456789", "8 dígitos")]         // largo
    [InlineData(TipoDocumentoIdentidad.Dni, "1234567A", "8 dígitos")]          // letra
    [InlineData(TipoDocumentoIdentidad.Dni, "١٢٣٤٥٦٧٨", "8 dígitos")]           // dígitos no ASCII
    [InlineData(TipoDocumentoIdentidad.CarnetExtranjeria, "12345678", "entre 9 y 12")]
    [InlineData(TipoDocumentoIdentidad.CarnetExtranjeria, "1234567890123", "entre 9 y 12")]
    [InlineData(TipoDocumentoIdentidad.CarnetExtranjeria, "CE-1234567", "entre 9 y 12")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB123", "entre 6 y 12")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB 12345", "entre 6 y 12")]
    public void Rechaza_numeros_con_formato_invalido_para_su_tipo(TipoDocumentoIdentidad tipo, string numero, string mensajeEsperado)
    {
        var excepcion = Assert.Throws<DocumentoIdentidadInvalidoException>(() => DocumentoIdentidad.Crear(tipo, numero));

        Assert.Contains(mensajeEsperado, excepcion.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void El_numero_es_obligatorio(string? numero)
    {
        var excepcion = Assert.Throws<DocumentoIdentidadInvalidoException>(() => DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, numero));

        Assert.Equal("El número de documento es obligatorio.", excepcion.Message);
    }

    [Fact]
    public void Rechaza_un_tipo_de_documento_inexistente()
    {
        Assert.Throws<DocumentoIdentidadInvalidoException>(() => DocumentoIdentidad.Crear((TipoDocumentoIdentidad)99, "12345678"));
    }

    [Fact]
    public void Dos_documentos_con_el_mismo_tipo_y_numero_son_iguales()
    {
        var a = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Pasaporte, "ab123456");
        var b = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Pasaporte, " AB123456 ");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void El_mismo_numero_con_distinto_tipo_es_otro_documento()
    {
        var carnet = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.CarnetExtranjeria, "123456789");
        var pasaporte = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Pasaporte, "123456789");

        Assert.NotEqual(carnet, pasaporte);
    }

    [Fact]
    public void Se_muestra_como_tipo_y_numero()
    {
        Assert.Equal("Dni 12345678", DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, "12345678").ToString());
    }

    [Fact]
    public void Su_excepcion_es_una_excepcion_de_dominio()
    {
        // La API traduce cualquier DomainException a HTTP 422 (ver GlobalExceptionHandler).
        Assert.IsAssignableFrom<DomainException>(new DocumentoIdentidadInvalidoException("x"));
    }
}
