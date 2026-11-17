using SocialManagement.Beneficiarios.Application.DTOs;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>Perfil de AutoMapper de Application: entidad Beneficiario → BeneficiarioDto.</summary>
[Trait("Category", "Unit")]
public sealed class BeneficiarioProfileTests
{
    [Fact]
    public void La_configuracion_de_mapeo_es_valida()
    {
        // Falla si un campo del DTO queda sin origen (p. ej. al agregar una propiedad nueva al DTO).
        MapperDePrueba.Configuracion().AssertConfigurationIsValid();
    }

    [Fact]
    public void Mapea_la_entidad_al_DTO_incluido_el_documento_y_el_estado_como_texto()
    {
        var beneficiario = BeneficiariosDePrueba.Crear("Ana María", "Quispe Mamani", "12345678");
        beneficiario.Desactivar();

        var dto = MapperDePrueba.Crear().Map<BeneficiarioDto>(beneficiario);

        Assert.Equal(
            new BeneficiarioDto(0, "Ana María", "Quispe Mamani", "Ana María Quispe Mamani", "Dni", "12345678", BeneficiariosDePrueba.Fecha, "Inactivo"),
            dto);
    }
}
