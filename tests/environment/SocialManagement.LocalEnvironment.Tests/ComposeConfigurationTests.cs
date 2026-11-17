using System.Text.RegularExpressions;

namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>
/// Validación estática de la infraestructura como código. No necesita Docker:
/// detecta errores de configuración antes de levantar los contenedores.
/// </summary>
[Trait("Category", "Infrastructure")]
public sealed partial class ComposeConfigurationTests
{
    private static readonly string[] MandatoryServices =
        ["sqlserver", "sqlserver-init", "rabbitmq", "beneficiarios-api", "solicitudes-api", "gateway", "frontend"];

    private readonly ComposeDocument _compose = ComposeDocument.Load(LocalEnvironment.ComposeFile);

    [Fact]
    public void Define_los_servicios_obligatorios_sin_perfil()
    {
        foreach (var name in MandatoryServices)
        {
            Assert.True(_compose.Services.ContainsKey(name), $"Falta el servicio '{name}'.");
            Assert.Empty(_compose.Services[name].Profiles);
        }
    }

    [Theory]
    [InlineData("localstack", "localstack")]
    [InlineData("ollama", "ollama")]
    public void Los_servicios_opcionales_pertenecen_a_un_perfil(string service, string profile)
    {
        Assert.Equal([profile], _compose.Services[service].Profiles);
    }

    [Fact]
    public void Ningun_servicio_obligatorio_depende_de_un_servicio_opcional()
    {
        foreach (var name in MandatoryServices)
        {
            Assert.Null(_compose.Services[name].DependencyCondition("localstack"));
            Assert.Null(_compose.Services[name].DependencyCondition("ollama"));
        }
    }

    [Fact]
    public void Los_servicios_de_larga_duracion_tienen_health_check()
    {
        foreach (var service in _compose.Services.Values.Where(s => s.Name != "sqlserver-init"))
        {
            Assert.True(service.HasHealthCheck, $"El servicio '{service.Name}' no define healthcheck.");
        }
    }

    [Fact]
    public void Todos_los_servicios_usan_la_red_dedicada()
    {
        Assert.Contains("social-management", _compose.Networks);
        Assert.All(_compose.Services.Values, service => Assert.Contains("social-management", service.Networks));
    }

    [Theory]
    [InlineData("sqlserver", "sqlserver-data")]
    [InlineData("rabbitmq", "rabbitmq-data")]
    [InlineData("localstack", "localstack-data")]
    [InlineData("ollama", "ollama-data")]
    public void Los_servicios_con_estado_usan_volumenes_persistentes(string service, string volume)
    {
        Assert.Contains(volume, _compose.Volumes);
        Assert.Contains(_compose.Services[service].Volumes, mount => mount.StartsWith($"{volume}:", StringComparison.Ordinal));
    }

    [Fact]
    public void Los_puertos_se_publican_solo_en_localhost_y_son_configurables()
    {
        foreach (var service in _compose.Services.Values)
        {
            Assert.All(service.Ports, port =>
            {
                Assert.StartsWith("127.0.0.1:${", port, StringComparison.Ordinal);
            });
        }
    }

    [Fact]
    public void Las_urls_internas_usan_el_DNS_de_Compose_y_no_localhost()
    {
        foreach (var service in _compose.Services.Values)
        {
            Assert.All(service.Environment, variable =>
                Assert.False(
                    variable.Value.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                    || variable.Value.Contains("127.0.0.1", StringComparison.Ordinal),
                    $"{service.Name}: '{variable.Key}' usa localhost. Dentro de un contenedor se debe usar el nombre del servicio."));
        }
    }

    [Theory]
    [InlineData("beneficiarios-api", "BeneficiariosDb", "SocialManagement_Beneficiarios")]
    [InlineData("solicitudes-api", "SolicitudesDb", "SocialManagement_Solicitudes")]
    public void Cada_microservicio_recibe_su_propia_base_y_la_infraestructura_por_DNS(string service, string connectionName, string database)
    {
        var environment = _compose.Services[service].Environment;

        Assert.Contains($"Server=sqlserver,1433;Database={database};", environment[$"ConnectionStrings__{connectionName}"], StringComparison.Ordinal);
        Assert.Equal("rabbitmq", environment["RabbitMq__Host"]);
        Assert.Equal("http://localstack:4566", environment["Aws__ServiceUrl"]);
        Assert.Equal("http://ollama:11434", environment["Ollama__Endpoint"]);
        Assert.Equal("${ASPNETCORE_ENVIRONMENT:-Development}", environment["ASPNETCORE_ENVIRONMENT"]);
    }

    [Fact]
    public void La_API_de_beneficiarios_aplica_sus_migraciones_al_iniciar()
    {
        // sqlserver-init crea la base vacía; las tablas las crean las migraciones de EF Core (sesión 03).
        var api = _compose.Services["beneficiarios-api"];

        Assert.Equal("true", api.Environment["Persistence__ApplyMigrationsAtStartup"]);
        Assert.Equal("service_completed_successfully", api.DependencyCondition("sqlserver-init"));
    }

    [Theory]
    [InlineData("beneficiarios-api")]
    [InlineData("solicitudes-api")]
    public void La_documentacion_de_cada_API_es_configurable_y_confia_en_el_Gateway(string service)
    {
        var environment = _compose.Services[service].Environment;

        Assert.Equal("${API_DOCS_ENABLED:-true}", environment["ApiDocumentation__Enabled"]);
        Assert.Equal("${API_DOCS_USER:-}", environment["ApiDocumentation__Access__UserName"]);
        Assert.Equal("${API_DOCS_PASSWORD:-}", environment["ApiDocumentation__Access__Password"]);
        // Sin redes de confianza, la API ignoraría X-Forwarded-Prefix y Swagger perdería el prefijo del Gateway.
        Assert.Contains(environment, variable => variable.Key.StartsWith("ForwardedHeaders__KnownNetworks__", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("beneficiarios", "beneficiarios-api")]
    [InlineData("solicitudes", "solicitudes-api")]
    public void El_Gateway_apunta_a_los_servicios_por_su_nombre_DNS(string cluster, string service)
    {
        var address = _compose.Services["gateway"].Environment[$"ReverseProxy__Clusters__{cluster}__Destinations__default__Address"];

        Assert.Equal($"http://{service}:8080/", address);
        Assert.True(_compose.Services.ContainsKey(service));
    }

    [Fact]
    public void El_orden_de_arranque_depende_de_la_disponibilidad_real()
    {
        var services = _compose.Services;

        Assert.Equal("service_healthy", services["sqlserver-init"].DependencyCondition("sqlserver"));
        foreach (var api in new[] { "beneficiarios-api", "solicitudes-api" })
        {
            Assert.Equal("service_completed_successfully", services[api].DependencyCondition("sqlserver-init"));
            Assert.Equal("service_healthy", services[api].DependencyCondition("rabbitmq"));
            Assert.Equal("service_healthy", services["gateway"].DependencyCondition(api));
        }

        Assert.Equal("service_healthy", services["frontend"].DependencyCondition("gateway"));
    }

    [Fact]
    public void Las_credenciales_provienen_de_variables_y_no_estan_escritas_en_el_compose()
    {
        var secretKeys = new[] { "PASSWORD", "PASS", "AUTH_TOKEN", "SECRET" };

        foreach (var service in _compose.Services.Values)
        {
            foreach (var (key, value) in service.Environment.Where(e => secretKeys.Any(s => e.Key.Contains(s, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.StartsWith("${", value, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Env_example_documenta_todas_las_variables_usadas_por_el_compose()
    {
        var composeText = File.ReadAllText(LocalEnvironment.ComposeFile);
        var used = ComposeVariable().Matches(composeText).Select(m => m.Groups[1].Value).ToHashSet();
        var documented = LocalEnvironment.ParseDotEnv(LocalEnvironment.EnvExampleFile).Keys.ToHashSet();

        Assert.Empty(used.Except(documented));
    }

    [Fact]
    public void Env_no_se_versiona()
    {
        var gitignore = File.ReadAllLines(Path.Combine(LocalEnvironment.Root, ".gitignore"));

        Assert.Contains(".env", gitignore);
    }

    [Theory]
    [InlineData("infra/sqlserver/init-databases.sh")]
    [InlineData("infra/sqlserver/init/01-create-databases.sql")]
    [InlineData("infra/localstack/init/ready.d/01-create-resources.sh")]
    [InlineData("infra/rabbitmq/rabbitmq.conf")]
    [InlineData("infra/docker/frontend/nginx.conf.template")]
    public void Los_archivos_que_se_montan_en_Linux_usan_finales_de_linea_LF(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(LocalEnvironment.Root, relativePath));

        Assert.DoesNotContain('\r', content);
    }

    [Fact]
    public void El_script_SQL_crea_ambas_bases_de_forma_idempotente()
    {
        var script = File.ReadAllText(Path.Combine(LocalEnvironment.Root, "infra/sqlserver/init/01-create-databases.sql"));

        foreach (var database in new[] { "SocialManagement_Beneficiarios", "SocialManagement_Solicitudes" })
        {
            Assert.Contains($"IF DB_ID(N'{database}') IS NULL", script, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("CREATE TABLE", script, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\$\{([A-Z0-9_]+)(?::?[-?][^}]*)?\}")]
    private static partial Regex ComposeVariable();
}
