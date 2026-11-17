using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>Datos de prueba ficticios para los casos de uso.</summary>
internal static class BeneficiariosDePrueba
{
    public static readonly DateTimeOffset Fecha = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    public static Beneficiario Crear(string nombres, string apellidos, string dni) =>
        Beneficiario.Registrar(nombres, apellidos, DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, dni), Fecha);

    public static async Task<Beneficiario> AgregarAsync(
        this FakeBeneficiarioRepository repository, string nombres, string apellidos, string dni)
    {
        var beneficiario = Crear(nombres, apellidos, dni);
        await repository.AgregarAsync(beneficiario, TestContext.Current.CancellationToken);
        return beneficiario;
    }
}
