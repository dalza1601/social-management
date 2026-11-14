using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SocialManagement.Solicitudes.Api.IntegrationTests;

/// <summary>
/// Levanta la API en memoria con una configuración de prueba completa y válida.
/// Ninguna dependencia externa se contacta durante el arranque.
/// </summary>
public class SolicitudesApiFactory : WebApplicationFactory<Program>
{
    public const string LocalStackUrl = "http://localstack.test:4566";

    private readonly Dictionary<string, string?> _overrides;
    private readonly string _environment;

    public SolicitudesApiFactory()
        : this(new Dictionary<string, string?>())
    {
    }

    protected SolicitudesApiFactory(Dictionary<string, string?> overrides, string environment = "Development")
    {
        _overrides = overrides;
        _environment = environment;
    }

    public static Dictionary<string, string?> ValidSettings() => new()
    {
        ["ConnectionStrings:SolicitudesDb"] =
            "Server=sqlserver.test,1433;Database=SocialManagement_Solicitudes;User Id=sa;Password=NotUsed_1;TrustServerCertificate=True;Connect Timeout=1",
        ["RabbitMq:Host"] = "rabbitmq.test",
        ["RabbitMq:UserName"] = "test-user",
        ["RabbitMq:Password"] = "test-password",
        ["Aws:UseLocalStack"] = "true",
        ["Aws:ServiceUrl"] = LocalStackUrl,
        ["Aws:AccessKey"] = "test",
        ["Aws:SecretKey"] = "test",
        ["Ollama:Enabled"] = "false",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        // Las fuentes agregadas aquí tienen la máxima prioridad (sobrescriben user-secrets y appsettings).
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = ValidSettings();
            foreach (var (key, value) in _overrides)
            {
                settings[key] = value;
            }

            configuration.AddInMemoryCollection(settings);
        });
    }
}
