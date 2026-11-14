using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using RabbitMQ.Client;

namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>
/// Comprueba los contenedores obligatorios en ejecución (docker compose up).
/// Se omiten salvo que SM_ENVIRONMENT_TESTS=true.
/// </summary>
[Trait("Category", "Environment")]
public sealed class CoreServicesLiveTests
{
    private const string SkipReason =
        "Requiere el entorno Docker en ejecución. Actívalo con SM_ENVIRONMENT_TESTS=true (o ejecuta scripts/check.ps1 -RunTests).";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public CoreServicesLiveTests() => Assert.SkipUnless(LocalEnvironment.LiveTestsEnabled, SkipReason);

    [Fact]
    public async Task SqlServer_tiene_las_dos_bases_de_datos()
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = $"127.0.0.1,{LocalEnvironment.GetPort("SQLSERVER_PORT", 14333)}",
            UserID = "sa",
            Password = LocalEnvironment.Get("MSSQL_SA_PASSWORD"),
            TrustServerCertificate = true,
            ConnectTimeout = 10,
        }.ConnectionString;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new SqlCommand("SELECT name FROM sys.databases WHERE name LIKE 'SocialManagement[_]%'", connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var databases = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            databases.Add(reader.GetString(0));
        }

        Assert.Contains("SocialManagement_Beneficiarios", databases);
        Assert.Contains("SocialManagement_Solicitudes", databases);
    }

    [Fact]
    public async Task RabbitMq_acepta_conexiones_AMQP()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = LocalEnvironment.GetPort("RABBITMQ_AMQP_PORT", 5672),
            VirtualHost = LocalEnvironment.Get("RABBITMQ_VHOST", "social-management"),
            UserName = LocalEnvironment.Get("RABBITMQ_USER"),
            Password = LocalEnvironment.Get("RABBITMQ_PASSWORD"),
        };

        await using var connection = await factory.CreateConnectionAsync("environment-tests", TestContext.Current.CancellationToken);

        Assert.True(connection.IsOpen);
    }

    [Fact]
    public async Task RabbitMq_publica_la_consola_de_administracion()
    {
        var port = LocalEnvironment.GetPort("RABBITMQ_MANAGEMENT_PORT", 15672);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{port}/api/overview");
        var credentials = $"{LocalEnvironment.Get("RABBITMQ_USER")}:{LocalEnvironment.Get("RABBITMQ_PASSWORD")}";
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

        using var response = await Http.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("BENEFICIARIOS_API_PORT", 8081)]
    [InlineData("SOLICITUDES_API_PORT", 8082)]
    [InlineData("GATEWAY_PORT", 8080)]
    public async Task Health_ready_reporta_todas_las_dependencias_sanas(string portVariable, int defaultPort)
    {
        var port = LocalEnvironment.GetPort(portVariable, defaultPort);

        var status = await GetJsonPropertyAsync($"http://localhost:{port}/health/ready", "status");

        Assert.Equal("Healthy", status);
    }

    [Theory]
    [InlineData("beneficiarios")]
    [InlineData("solicitudes")]
    public async Task El_Gateway_enruta_a_cada_microservicio(string service)
    {
        var port = LocalEnvironment.GetPort("GATEWAY_PORT", 8080);

        var name = await GetJsonPropertyAsync($"http://localhost:{port}/api/{service}/version", "service");

        Assert.Equal($"{service}-api", name);
    }

    [Theory]
    [InlineData("beneficiarios")]
    [InlineData("solicitudes")]
    public async Task Swagger_de_cada_microservicio_es_accesible_a_traves_del_Gateway(string service)
    {
        var gateway = $"http://localhost:{LocalEnvironment.GetPort("GATEWAY_PORT", 8080)}";
        var baseUrl = $"{gateway}/api/{service}";

        using var page = await Http.GetAsync($"{baseUrl}/swagger/index.html", TestContext.Current.CancellationToken);
        var documentJson = await Http.GetStringAsync($"{baseUrl}/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        using var document = JsonDocument.Parse(documentJson);
        var server = document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString();
        // El documento debe anunciar la URL pública (Gateway + prefijo), no la interna del contenedor.
        Assert.Equal(baseUrl, server?.TrimEnd('/'));
    }

    [Fact]
    public async Task El_frontend_responde_y_alcanza_el_Gateway()
    {
        var port = LocalEnvironment.GetPort("FRONTEND_PORT", 4200);

        var index = await Http.GetStringAsync($"http://localhost:{port}/", TestContext.Current.CancellationToken);
        var gatewayStatus = await GetJsonPropertyAsync($"http://localhost:{port}/gateway/health", "status");

        Assert.Contains("<app-root", index, StringComparison.Ordinal);
        Assert.Equal("Healthy", gatewayStatus);
    }

    private static async Task<string?> GetJsonPropertyAsync(string url, string property)
    {
        using var response = await Http.GetAsync(url, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"{url} respondió {(int)response.StatusCode}: {body}");

        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty(property).GetString();
    }
}
