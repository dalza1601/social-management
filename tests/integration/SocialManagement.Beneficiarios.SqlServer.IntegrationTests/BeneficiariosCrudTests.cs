using System.Net;
using Microsoft.EntityFrameworkCore;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.SqlServer.IntegrationTests;

/// <summary>
/// CRUD de beneficiarios de punta a punta: HTTP → controller → caso de uso → EF Core → SQL Server.
/// Además de la respuesta HTTP, se comprueba lo que quedó realmente guardado en la base de datos.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "SqlServer")]
public sealed class BeneficiariosCrudTests(SqlServerBeneficiariosApiFactory factory) : IClassFixture<SqlServerBeneficiariosApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Cliente autenticado como Administrador (puede todas las operaciones). Ver BeneficiariosAutorizacionTests.</summary>
    private async Task<HttpClient> ClienteAsync()
    {
        Assert.SkipUnless(SqlServerBeneficiariosApiFactory.IsConfigured, SqlServerBeneficiariosApiFactory.SkipReason);
        return await factory.CreateAdminClientAsync();
    }

    [Fact]
    public async Task Las_migraciones_se_aplicaron_al_iniciar_la_API()
    {
        await ClienteAsync();   // arranca la API (y con ella las migraciones)

        var (aplicadas, pendientes) = await factory.QueryDatabaseAsync(async db =>
            (await db.Database.GetAppliedMigrationsAsync(Ct), await db.Database.GetPendingMigrationsAsync(Ct)));

        Assert.Contains(aplicadas, m => m.EndsWith("_InicialBeneficiarios", StringComparison.Ordinal));
        Assert.Empty(pendientes);
    }

    [Fact]
    public async Task Registrar_consultar_actualizar_y_desactivar()
    {
        var client = await ClienteAsync();
        var dni = BeneficiariosApiClient.NuevoDni();

        // POST → 201 + Location
        var creado = await client.RegistrarAsync(BeneficiariosApiClient.Datos("Ana María", "Quispe Mamani", dni));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var registro = (await creado.LeerAsync<BeneficiarioDto>()).Data!;
        Assert.Equal($"/api/v1/beneficiarios/{registro.Id}", creado.Headers.Location?.AbsolutePath);

        // GET Location → 200
        var consulta = await client.GetAsync(creado.Headers.Location, Ct);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal("Ana María Quispe Mamani", (await consulta.LeerAsync<BeneficiarioDto>()).Data?.NombreCompleto);

        // PUT → 200
        var actualizado = await client.ActualizarAsync(registro.Id, BeneficiariosApiClient.Datos("Ana María", "Quispe Rojas", dni));
        Assert.Equal(HttpStatusCode.OK, actualizado.StatusCode);
        Assert.Equal("Quispe Rojas", (await actualizado.LeerAsync<BeneficiarioDto>()).Data?.Apellidos);

        // PATCH desactivar → 200 (baja lógica)
        var desactivado = await client.DesactivarAsync(registro.Id);
        Assert.Equal(HttpStatusCode.OK, desactivado.StatusCode);
        Assert.Equal("Inactivo", (await desactivado.LeerAsync<BeneficiarioDto>()).Data?.Estado);

        // Lo que quedó en SQL Server (consulta directa, sin pasar por la API).
        var guardado = await factory.QueryDatabaseAsync(db =>
            db.Beneficiarios.AsNoTracking().SingleAsync(b => b.Id == registro.Id, Ct));
        Assert.Equal("Ana María", guardado.Nombres);   // tildes conservadas (nvarchar)
        Assert.Equal("Quispe Rojas", guardado.Apellidos);
        Assert.Equal(EstadoBeneficiario.Inactivo, guardado.Estado);
        Assert.Equal(DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, dni), guardado.Documento);
        // Auditoría completada por el interceptor de EF Core (no por el controller ni el caso de uso).
        Assert.NotEqual(default, guardado.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, guardado.CreatedAtUtc.Kind);
        Assert.NotNull(guardado.UpdatedAtUtc);
        Assert.True(guardado.UpdatedAtUtc >= guardado.CreatedAtUtc);
        // Sesión 04: el usuario del token JWT queda como autor de la creación y de la última modificación.
        Assert.Equal(SqlServerBeneficiariosApiFactory.AdminUser, guardado.CreatedBy);
        Assert.Equal(SqlServerBeneficiariosApiFactory.AdminUser, guardado.UpdatedBy);
    }

    [Fact]
    public async Task Registrar_un_documento_existente_responde_409_y_no_duplica()
    {
        var client = await ClienteAsync();
        var dni = BeneficiariosApiClient.NuevoDni();
        Assert.Equal(HttpStatusCode.Created, (await client.RegistrarAsync(BeneficiariosApiClient.Datos("Luis", "Huamán", dni))).StatusCode);

        var duplicado = await client.RegistrarAsync(BeneficiariosApiClient.Datos("Pedro", "Rojas", dni));

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        var cuerpo = await duplicado.LeerAsync<object>();
        Assert.False(cuerpo.Success);
        Assert.Equal($"Ya existe un beneficiario registrado con el documento Dni {dni}.", Assert.Single(cuerpo.Errors).Message);

        // Con un tipo "owned" se comparan sus propiedades (EF Core no traduce la igualdad del objeto completo).
        Assert.Equal(1, await factory.QueryDatabaseAsync(db => db.Beneficiarios.CountAsync(
            b => b.Documento.Tipo == TipoDocumentoIdentidad.Dni && b.Documento.Numero == dni, Ct)));
    }

    [Fact]
    public async Task Actualizar_con_el_documento_de_otro_beneficiario_responde_409()
    {
        var client = await ClienteAsync();
        var dniLuis = BeneficiariosApiClient.NuevoDni();
        await client.RegistrarAsync(BeneficiariosApiClient.Datos("Luis", "Huamán", dniLuis));
        var ana = (await (await client.RegistrarAsync(BeneficiariosApiClient.Datos("Ana", "Quispe", BeneficiariosApiClient.NuevoDni())))
            .LeerAsync<BeneficiarioDto>()).Data!;

        var respuesta = await client.ActualizarAsync(ana.Id, BeneficiariosApiClient.Datos("Ana", "Quispe", dniLuis));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_indice_unico_de_la_base_de_datos_tambien_impide_duplicados()
    {
        await ClienteAsync();
        var dni = BeneficiariosApiClient.NuevoDni();
        var fecha = DateTimeOffset.UtcNow;

        // Saltándose el caso de uso (como dos peticiones simultáneas): la restricción UX_Beneficiarios_Documento responde.
        // Cada entidad recibe SU PROPIA instancia del Value Object: EF Core no permite compartir una instancia "owned".
        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => factory.QueryDatabaseAsync(async db =>
        {
            db.Beneficiarios.Add(Beneficiario.Registrar("Ana", "Quispe", DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, dni), fecha));
            db.Beneficiarios.Add(Beneficiario.Registrar("Rosa", "Vargas", DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, dni), fecha));
            return await db.SaveChangesAsync(Ct);
        }));

        Assert.Contains("UX_Beneficiarios_Documento", excepcion.InnerException?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Datos_invalidos_responden_400_con_un_error_por_campo_y_no_se_guardan()
    {
        var client = await ClienteAsync();
        var antes = await factory.QueryDatabaseAsync(db => db.Beneficiarios.CountAsync(Ct));

        var respuesta = await client.RegistrarAsync(BeneficiariosApiClient.Datos("Ana123", "", "123"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.LeerAsync<object>();
        Assert.Equal(["Apellidos", "Nombres", "NumeroDocumento"], cuerpo.Errors.Select(e => e.Field).Order());
        Assert.Equal(antes, await factory.QueryDatabaseAsync(db => db.Beneficiarios.CountAsync(Ct)));
    }

    [Fact]
    public async Task Un_identificador_inexistente_responde_404_en_consultar_actualizar_y_desactivar()
    {
        var client = await ClienteAsync();
        const int id = int.MaxValue;   // nunca asignado por la columna IDENTITY en estas pruebas

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{BeneficiariosApiClient.Ruta}/{id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.ActualizarAsync(id, BeneficiariosApiClient.Datos("Ana", "Quispe", BeneficiariosApiClient.NuevoDni()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DesactivarAsync(id)).StatusCode);
    }

    [Fact]
    public async Task SQL_Server_asigna_Ids_enteros_consecutivos()
    {
        var client = await ClienteAsync();

        var primero = (await (await client.RegistrarAsync(BeneficiariosApiClient.Datos("Rosa", "Vargas", BeneficiariosApiClient.NuevoDni())))
            .LeerAsync<BeneficiarioDto>()).Data!;
        var segundo = (await (await client.RegistrarAsync(BeneficiariosApiClient.Datos("Carlos", "Flores", BeneficiariosApiClient.NuevoDni())))
            .LeerAsync<BeneficiarioDto>()).Data!;

        Assert.True(primero.Id > 0);
        Assert.True(segundo.Id > primero.Id);   // IDENTITY(1,1): crece en cada INSERT
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public async Task Un_identificador_no_positivo_responde_400(string id)
    {
        var respuesta = await (await ClienteAsync()).GetAsync($"{BeneficiariosApiClient.Ruta}/{id}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("Id", Assert.Single((await respuesta.LeerAsync<object>()).Errors).Field);
    }

    [Fact]
    public async Task Un_identificador_que_no_es_entero_no_coincide_con_la_ruta_y_responde_404()
    {
        // La restricción {id:int} solo acepta enteros: un GUID u otro texto no llega al controller.
        var respuesta = await (await ClienteAsync()).GetAsync($"{BeneficiariosApiClient.Ruta}/abc", Ct);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
