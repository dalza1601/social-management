using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialManagement.Beneficiarios.Api.Contracts.Authentication;
using SocialManagement.Beneficiarios.Infrastructure.Persistence;

namespace SocialManagement.Beneficiarios.SqlServer.IntegrationTests;

/// <summary>
/// Levanta la API en memoria contra una base de datos SQL Server REAL, temporal y exclusiva de cada
/// clase de prueba (nombre único). La API aplica las migraciones al iniciar
/// (Persistence:ApplyMigrationsAtStartup = true), igual que en Docker Compose; al terminar, la base se elimina.
/// </summary>
public sealed class SqlServerBeneficiariosApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Variable de entorno con la cadena de conexión al servidor (sin Database).</summary>
    public const string ServerConnectionVariable = "SM_SQLSERVER_TESTS";

    public static readonly string SkipReason =
        $"Pruebas con SQL Server omitidas: define {ServerConnectionVariable} con la cadena de conexión al servidor " +
        @"(p. ej. Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True).";

    private static readonly string? ServerConnection = Environment.GetEnvironmentVariable(ServerConnectionVariable);

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ServerConnection);

    // Usuarios demo de estas pruebas (POST /api/auth/dev-token, solo Development).
    public const string AdminUser = "admin.sql";
    public const string AdminPassword = "Clave-Admin-SqlServer";
    public const string OperadorUser = "operador.sql";
    public const string OperadorPassword = "Clave-Operador-SqlServer";

    /// <summary>
    /// Nombre único (fecha y hora + número aleatorio): varias clases y ejecuciones pueden correr en paralelo
    /// sin interferir.
    /// </summary>
    public string DatabaseName { get; } =
        $"SocialManagement_Beneficiarios_Test_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Random.Shared.Next(100_000, 1_000_000)}";

    public string ConnectionString => new SqlConnectionStringBuilder(ServerConnection)
    {
        InitialCatalog = DatabaseName,
        ConnectTimeout = 30,
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:BeneficiariosDb"] = ConnectionString,
            ["Persistence:ApplyMigrationsAtStartup"] = "true",
            // El resto de dependencias no se contactan durante estas pruebas: valores de prueba válidos.
            ["RabbitMq:Host"] = "rabbitmq.test",
            ["RabbitMq:UserName"] = "test-user",
            ["RabbitMq:Password"] = "test-password",
            ["Aws:UseLocalStack"] = "true",
            ["Aws:ServiceUrl"] = "http://localstack.test:4566",
            ["Aws:AccessKey"] = "test",
            ["Aws:SecretKey"] = "test",
            ["Ollama:Enabled"] = "false",
            // Sesión 04: clave JWT y usuarios demo (un equipo real los tendría en user-secrets o variables de entorno).
            ["Jwt:SigningKey"] = "Clave-De-Firma-Solo-Para-Pruebas-SqlServer-01",
            ["DevelopmentTokens:Users:0:UserName"] = AdminUser,
            ["DevelopmentTokens:Users:0:Password"] = AdminPassword,
            ["DevelopmentTokens:Users:0:Roles:0"] = "Administrador",
            ["DevelopmentTokens:Users:1:UserName"] = OperadorUser,
            ["DevelopmentTokens:Users:1:Password"] = OperadorPassword,
            ["DevelopmentTokens:Users:1:Roles:0"] = "Operador",
        }));
    }

    /// <summary>
    /// Cliente HTTP autenticado: pide un token a POST /api/auth/dev-token con las credenciales indicadas
    /// (como haría una persona en Swagger) y lo envía en cada petición (Authorization: Bearer ...).
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string userName, string password)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/dev-token", new { userName, password }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<DevTokenResponse>(TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public Task<HttpClient> CreateAdminClientAsync() => CreateAuthenticatedClientAsync(AdminUser, AdminPassword);

    public Task<HttpClient> CreateOperadorClientAsync() => CreateAuthenticatedClientAsync(OperadorUser, OperadorPassword);

    /// <summary>Ejecuta una consulta directa a la base de datos (fuera de la API) con un DbContext nuevo.</summary>
    public async Task<T> QueryDatabaseAsync<T>(Func<BeneficiariosDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BeneficiariosDbContext>();
        return await query(db);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (IsConfigured)
        {
            // EnsureDeleted cierra las conexiones del pool y elimina la base temporal (no falla si no existe).
            var options = new DbContextOptionsBuilder<BeneficiariosDbContext>().UseSqlServer(ConnectionString).Options;
            await using var db = new BeneficiariosDbContext(options);
            await db.Database.EnsureDeletedAsync();
        }
    }
}
