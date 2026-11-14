using FluentValidation.TestHelper;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;
using SocialManagement.Beneficiarios.Application.Validators;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>
/// Validación de entrada con FluentValidation (TestValidate / ShouldHaveValidationErrorFor).
/// Todos los datos son ficticios.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RegistrarBeneficiarioCommandValidatorTests
{
    private readonly RegistrarBeneficiarioCommandValidator _validator = new();

    private static RegistrarBeneficiarioCommand Valido() =>
        new("Ana María", "Quispe Mamani", TipoDocumentoIdentidad.Dni, "12345678");

    [Fact]
    public void Un_comando_valido_no_tiene_errores()
    {
        _validator.TestValidate(Valido()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Los nombres son obligatorios.")]
    [InlineData("   ", "Los nombres son obligatorios.")]
    [InlineData("Ana123", "Los nombres solo pueden contener letras, espacios, apóstrofos, guiones y puntos.")]
    public void Valida_los_nombres(string nombres, string mensaje)
    {
        _validator.TestValidate(Valido() with { Nombres = nombres })
            .ShouldHaveValidationErrorFor(c => c.Nombres)
            .WithErrorMessage(mensaje)
            .Only();
    }

    [Fact]
    public void Valida_la_longitud_maxima_con_la_constante_del_dominio()
    {
        var largo = new string('a', Beneficiario.LongitudMaximaNombre + 1);

        _validator.TestValidate(Valido() with { Apellidos = largo })
            .ShouldHaveValidationErrorFor(c => c.Apellidos)
            .WithErrorMessage("Los apellidos no pueden superar 100 caracteres.");
    }

    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "1234567", "El DNI debe tener exactamente 8 dígitos.")]
    [InlineData(TipoDocumentoIdentidad.CarnetExtranjeria, "12345678", "El carné de extranjería debe tener entre 9 y 12 caracteres alfanuméricos.")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB1", "El pasaporte debe tener entre 6 y 12 caracteres alfanuméricos.")]
    [InlineData(TipoDocumentoIdentidad.Dni, "", "El número de documento es obligatorio.")]
    public void Valida_el_numero_segun_el_tipo_de_documento(TipoDocumentoIdentidad tipo, string numero, string mensaje)
    {
        _validator.TestValidate(Valido() with { TipoDocumento = tipo, NumeroDocumento = numero })
            .ShouldHaveValidationErrorFor(c => c.NumeroDocumento)
            .WithErrorMessage(mensaje)
            .Only();
    }

    [Fact]
    public void Un_tipo_de_documento_inexistente_solo_informa_el_tipo()
    {
        var resultado = _validator.TestValidate(Valido() with { TipoDocumento = (TipoDocumentoIdentidad)99 });

        resultado.ShouldHaveValidationErrorFor(c => c.TipoDocumento).WithErrorMessage("El tipo de documento no es válido.");
        resultado.ShouldNotHaveValidationErrorFor(c => c.NumeroDocumento);
    }

    /// <summary>
    /// El validador (Application) y las invariantes (Domain) deben aceptar y rechazar EXACTAMENTE los mismos datos.
    /// Si alguien cambia una regla en un solo lugar, esta prueba falla.
    /// </summary>
    [Theory]
    [InlineData("Ana María", "Quispe", TipoDocumentoIdentidad.Dni, "12345678")]
    [InlineData("  José   Luis ", "D'Angelo", TipoDocumentoIdentidad.Dni, " 87654321 ")]
    [InlineData("Ma. del Carmen", "Pérez-Ríos", TipoDocumentoIdentidad.Pasaporte, "ab123456")]
    [InlineData("Ñahui", "Huamán", TipoDocumentoIdentidad.CarnetExtranjeria, "001234567")]
    [InlineData("Ana123", "Quispe", TipoDocumentoIdentidad.Dni, "12345678")]
    [InlineData("Ana", "", TipoDocumentoIdentidad.Dni, "12345678")]
    [InlineData("Ana", "Quispe", TipoDocumentoIdentidad.Dni, "1234567A")]
    [InlineData("Ana", "Quispe", TipoDocumentoIdentidad.CarnetExtranjeria, "CE-1234567")]
    [InlineData("Ana", "Quispe", TipoDocumentoIdentidad.Pasaporte, "AB 12345")]
    [InlineData("Ana", "Quispe", (TipoDocumentoIdentidad)99, "12345678")]
    public void El_validador_y_el_dominio_coinciden(string nombres, string apellidos, TipoDocumentoIdentidad tipo, string numero)
    {
        var validoSegunFluentValidation = _validator.Validate(new RegistrarBeneficiarioCommand(nombres, apellidos, tipo, numero)).IsValid;

        Assert.Equal(EsValidoSegunElDominio(nombres, apellidos, tipo, numero), validoSegunFluentValidation);
    }

    private static bool EsValidoSegunElDominio(string nombres, string apellidos, TipoDocumentoIdentidad tipo, string numero)
    {
        try
        {
            var documento = DocumentoIdentidad.Crear(tipo, numero);
            Beneficiario.Registrar(nombres, apellidos, documento, DateTimeOffset.UtcNow);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
