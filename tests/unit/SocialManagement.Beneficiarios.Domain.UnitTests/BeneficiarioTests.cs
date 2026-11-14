using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Domain.UnitTests;

/// <summary>Invariantes de la entidad Beneficiario. Todos los datos son ficticios.</summary>
[Trait("Category", "Unit")]
public sealed class BeneficiarioTests
{
    private static readonly DateTimeOffset FechaRegistro = new(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);
    private static readonly DocumentoIdentidad Documento = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, "12345678");

    [Fact]
    public void Registrar_crea_un_beneficiario_activo_con_los_datos_normalizados()
    {
        var beneficiario = Beneficiario.Registrar("  Ana   María ", "Quispe  Mamani", Documento, FechaRegistro);

        Assert.NotEqual(Guid.Empty, beneficiario.Id);
        Assert.Equal("Ana María", beneficiario.Nombres);
        Assert.Equal("Quispe Mamani", beneficiario.Apellidos);
        Assert.Equal("Ana María Quispe Mamani", beneficiario.NombreCompleto);
        Assert.Equal(Documento, beneficiario.Documento);
        Assert.Equal(FechaRegistro, beneficiario.FechaRegistroUtc);
        Assert.Equal(EstadoBeneficiario.Activo, beneficiario.Estado);
        Assert.True(beneficiario.EstaActivo);
    }

    [Fact]
    public void Cada_registro_recibe_un_Id_distinto_de_version_7()
    {
        var primero = Beneficiario.Registrar("Ana", "Quispe", Documento, FechaRegistro);
        var segundo = Beneficiario.Registrar("Ana", "Quispe", Documento, FechaRegistro);

        Assert.NotEqual(primero.Id, segundo.Id);
        Assert.Equal(7, primero.Id.Version);
    }

    [Fact]
    public void La_fecha_de_registro_se_guarda_en_UTC()
    {
        var fechaLima = new DateTimeOffset(2026, 11, 13, 10, 30, 0, TimeSpan.FromHours(-5));

        var beneficiario = Beneficiario.Registrar("Ana", "Quispe", Documento, fechaLima);

        Assert.Equal(TimeSpan.Zero, beneficiario.FechaRegistroUtc.Offset);
        Assert.Equal(fechaLima, beneficiario.FechaRegistroUtc);   // mismo instante
        Assert.Equal(15, beneficiario.FechaRegistroUtc.Hour);
    }

    [Theory]
    [InlineData("José Luis")]
    [InlineData("Ñahui")]
    [InlineData("D'Angelo")]
    [InlineData("Ana-Lucía")]
    [InlineData("Ma. del Carmen")]
    public void Acepta_nombres_con_tildes_enes_apostrofos_guiones_y_puntos(string nombres)
    {
        var beneficiario = Beneficiario.Registrar(nombres, "Pérez", Documento, FechaRegistro);

        Assert.Equal(nombres, beneficiario.Nombres);
    }

    [Theory]
    [InlineData(null, "obligatorios")]
    [InlineData("", "obligatorios")]
    [InlineData("    ", "obligatorios")]
    [InlineData("Juan123", "solo pueden contener letras")]
    [InlineData("Ana@Maria", "solo pueden contener letras")]
    public void Rechaza_nombres_invalidos(string? nombres, string mensajeEsperado)
    {
        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(
            () => Beneficiario.Registrar(nombres, "Quispe", Documento, FechaRegistro));

        Assert.Contains("nombres", excepcion.Message, StringComparison.Ordinal);
        Assert.Contains(mensajeEsperado, excepcion.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rechaza_apellidos_vacios()
    {
        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(
            () => Beneficiario.Registrar("Ana", " ", Documento, FechaRegistro));

        Assert.Equal("Los apellidos son obligatorios.", excepcion.Message);
    }

    [Fact]
    public void Acepta_hasta_100_caracteres_y_rechaza_101()
    {
        var limite = new string('a', Beneficiario.LongitudMaximaNombre);

        var beneficiario = Beneficiario.Registrar(limite, "Quispe", Documento, FechaRegistro);
        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(
            () => Beneficiario.Registrar(limite + "a", "Quispe", Documento, FechaRegistro));

        Assert.Equal(100, beneficiario.Nombres.Length);
        Assert.Contains("no pueden superar 100 caracteres", excepcion.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void El_documento_es_obligatorio()
    {
        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(
            () => Beneficiario.Registrar("Ana", "Quispe", null, FechaRegistro));

        Assert.Equal("El documento de identidad es obligatorio.", excepcion.Message);
    }

    [Fact]
    public void La_fecha_de_registro_es_obligatoria()
    {
        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(
            () => Beneficiario.Registrar("Ana", "Quispe", Documento, default));

        Assert.Equal("La fecha de registro es obligatoria.", excepcion.Message);
    }

    [Fact]
    public void Desactivar_y_activar_cambian_el_estado_y_son_idempotentes()
    {
        var beneficiario = Beneficiario.Registrar("Ana", "Quispe", Documento, FechaRegistro);

        beneficiario.Desactivar();
        beneficiario.Desactivar();
        Assert.Equal(EstadoBeneficiario.Inactivo, beneficiario.Estado);
        Assert.False(beneficiario.EstaActivo);

        beneficiario.Activar();
        Assert.Equal(EstadoBeneficiario.Activo, beneficiario.Estado);
    }
}
