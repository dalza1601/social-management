using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Domain.UnitTests;

/// <summary>
/// Beneficiario.ActualizarDatos: aplica las mismas invariantes que Registrar y es atómico
/// (si un dato no es válido, la entidad no cambia). Todos los datos son ficticios.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BeneficiarioActualizarDatosTests
{
    private static readonly DocumentoIdentidad DniOriginal = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, "12345678");

    private static Beneficiario Ana() =>
        Beneficiario.Registrar("Ana", "Quispe", DniOriginal, new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Actualiza_y_normaliza_los_datos_personales()
    {
        var ana = Ana();
        var pasaporte = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Pasaporte, "AB123456");

        ana.ActualizarDatos("  Ana   María ", "Quispe  Rojas", pasaporte);

        Assert.Equal("Ana María", ana.Nombres);
        Assert.Equal("Quispe Rojas", ana.Apellidos);
        Assert.Equal(pasaporte, ana.Documento);
    }

    [Fact]
    public void No_cambia_el_Id_la_fecha_de_registro_ni_el_estado()
    {
        var ana = Ana();
        var (id, fecha, estado) = (ana.Id, ana.FechaRegistroUtc, ana.Estado);

        ana.ActualizarDatos("Ana María", "Quispe", DniOriginal);

        Assert.Equal(id, ana.Id);
        Assert.Equal(fecha, ana.FechaRegistroUtc);
        Assert.Equal(estado, ana.Estado);
    }

    [Theory]
    [InlineData("", "Quispe", "Los nombres son obligatorios.")]
    [InlineData("Ana", "  ", "Los apellidos son obligatorios.")]
    [InlineData("Ana123", "Quispe", "Los nombres solo pueden contener letras, espacios, apóstrofos, guiones y puntos.")]
    public void Rechaza_datos_invalidos_sin_modificar_la_entidad(string nombres, string apellidos, string mensaje)
    {
        var ana = Ana();
        var pasaporte = DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Pasaporte, "AB123456");

        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(() => ana.ActualizarDatos(nombres, apellidos, pasaporte));

        Assert.Equal(mensaje, excepcion.Message);
        Assert.Equal("Ana", ana.Nombres);
        Assert.Equal("Quispe", ana.Apellidos);
        Assert.Equal(DniOriginal, ana.Documento);   // atómico: tampoco cambió el documento
    }

    [Fact]
    public void El_documento_es_obligatorio()
    {
        var excepcion = Assert.Throws<BeneficiarioInvalidoException>(() => Ana().ActualizarDatos("Ana", "Quispe", null));

        Assert.Equal("El documento de identidad es obligatorio.", excepcion.Message);
    }
}
