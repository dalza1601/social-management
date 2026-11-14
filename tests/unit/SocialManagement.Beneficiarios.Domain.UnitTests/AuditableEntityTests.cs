using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Domain.UnitTests;

/// <summary>Campos de auditoría de <see cref="AuditableEntity"/> (se prueban a través de Beneficiario).</summary>
[Trait("Category", "Unit")]
public sealed class AuditableEntityTests
{
    private static Beneficiario NuevoBeneficiario() => Beneficiario.Registrar(
        "Ana", "Quispe",
        DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, "12345678"),
        new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void MarkCreated_registra_fecha_y_usuario_de_creacion()
    {
        var beneficiario = NuevoBeneficiario();
        var fecha = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);

        beneficiario.MarkCreated(fecha, "usuario.prueba");

        Assert.Equal(fecha, beneficiario.CreatedAtUtc);
        Assert.Equal("usuario.prueba", beneficiario.CreatedBy);
        Assert.Equal("usuario.prueba", beneficiario.UpdatedBy);
        Assert.Null(beneficiario.UpdatedAtUtc);
    }

    [Fact]
    public void MarkUpdated_registra_fecha_y_usuario_de_modificacion_sin_tocar_la_creacion()
    {
        var beneficiario = NuevoBeneficiario();
        var creado = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
        beneficiario.MarkCreated(creado, "creador");

        beneficiario.MarkUpdated(creado.AddHours(3), "editor");

        Assert.Equal(creado.AddHours(3), beneficiario.UpdatedAtUtc);
        Assert.Equal("editor", beneficiario.UpdatedBy);
        Assert.Equal(creado, beneficiario.CreatedAtUtc);
        Assert.Equal("creador", beneficiario.CreatedBy);
    }

    [Fact]
    public void Las_fechas_se_guardan_siempre_como_UTC()
    {
        var beneficiario = NuevoBeneficiario();
        var sinZona = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Unspecified);

        beneficiario.MarkCreated(sinZona, null);
        beneficiario.MarkUpdated(sinZona, null);

        Assert.Equal(DateTimeKind.Utc, beneficiario.CreatedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, beneficiario.UpdatedAtUtc!.Value.Kind);
    }
}
