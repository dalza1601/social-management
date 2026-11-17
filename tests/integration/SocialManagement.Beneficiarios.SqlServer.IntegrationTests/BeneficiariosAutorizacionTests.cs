using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Domain.Enums;

namespace SocialManagement.Beneficiarios.SqlServer.IntegrationTests;

/// <summary>
/// Sesión 04: autorización de punta a punta con tokens reales (POST /api/auth/dev-token) y SQL Server.
/// Además del código HTTP se comprueba la base de datos: una petición rechazada no debe cambiar nada, y la
/// auditoría (CreatedBy / UpdatedBy) debe registrar al usuario del token.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "SqlServer")]
public sealed class BeneficiariosAutorizacionTests(SqlServerBeneficiariosApiFactory factory) : IClassFixture<SqlServerBeneficiariosApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static void RequiereSqlServer() =>
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);

    [Fact]
    public async Task Sin_token_responde_401_y_no_guarda_nada()
    {
        RequiereSqlServer();
        var anonimo = factory.CreateClient();
        var dni = BeneficiariosApiClient.NuevoDni();

        var respuesta = await anonimo.RegistrarAsync(BeneficiariosApiClient.Datos("Rosa", "Vargas", dni));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.False(await factory.QueryDatabaseAsync(db =>
            db.Beneficiarios.AnyAsync(b => b.Documento.Numero == dni, Ct)));
    }

    [Fact]
    public async Task El_operador_registra_y_queda_como_autor()
    {
        RequiereSqlServer();
        var operador = await factory.CreateOperadorClientAsync();

        var respuesta = await operador.RegistrarAsync(BeneficiariosApiClient.Datos("Luis", "Huamán", BeneficiariosApiClient.NuevoDni()));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.LeerAsync<BeneficiarioDto>()).Data!.Id;
        var guardado = await factory.QueryDatabaseAsync(db => db.Beneficiarios.AsNoTracking().SingleAsync(b => b.Id == id, Ct));
        Assert.Equal(SqlServerBeneficiariosApiFactory.OperadorUser, guardado.CreatedBy);
    }

    [Fact]
    public async Task El_operador_no_puede_desactivar_y_el_administrador_si()
    {
        RequiereSqlServer();
        var operador = await factory.CreateOperadorClientAsync();
        var admin = await factory.CreateAdminClientAsync();
        var creado = await operador.RegistrarAsync(BeneficiariosApiClient.Datos("Carmen", "Flores", BeneficiariosApiClient.NuevoDni()));
        var id = (await creado.LeerAsync<BeneficiarioDto>()).Data!.Id;

        // Operador → 403: el registro sigue intacto en la base de datos.
        var rechazado = await operador.DesactivarAsync(id);
        Assert.Equal(HttpStatusCode.Forbidden, rechazado.StatusCode);
        var intacto = await factory.QueryDatabaseAsync(db => db.Beneficiarios.AsNoTracking().SingleAsync(b => b.Id == id, Ct));
        Assert.Equal(EstadoBeneficiario.Activo, intacto.Estado);
        Assert.Null(intacto.UpdatedAtUtc);

        // Administrador → 200: queda como autor de la modificación; el autor de la creación no cambia.
        var desactivado = await admin.DesactivarAsync(id);
        Assert.Equal(HttpStatusCode.OK, desactivado.StatusCode);
        var final = await factory.QueryDatabaseAsync(db => db.Beneficiarios.AsNoTracking().SingleAsync(b => b.Id == id, Ct));
        Assert.Equal(EstadoBeneficiario.Inactivo, final.Estado);
        Assert.Equal(SqlServerBeneficiariosApiFactory.OperadorUser, final.CreatedBy);
        Assert.Equal(SqlServerBeneficiariosApiFactory.AdminUser, final.UpdatedBy);
    }

    [Fact]
    public async Task Las_credenciales_demo_incorrectas_no_obtienen_token()
    {
        RequiereSqlServer();

        var respuesta = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/dev-token",
            new { userName = SqlServerBeneficiariosApiFactory.AdminUser, password = "clave-incorrecta" },
            Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
