using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Documentación de la API: Swagger UI, documento OpenAPI, configuración por ambiente,
/// control de acceso y funcionamiento detrás del Gateway.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ApiDocumentationTests(BeneficiariosApiFactory factory) : IClassFixture<BeneficiariosApiFactory>
{
    private const string ServiceName = "beneficiarios-api";
    private const string GatewayPrefix = "/api/beneficiarios";
    private const string DocsUser = "docs-reader";
    private const string DocsPassword = "Una-Clave-De-Prueba-Larga";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- Development

    [Fact]
    public async Task Swagger_UI_se_publica_y_apunta_al_documento_con_una_URL_relativa()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var redirect = await client.GetAsync("/swagger", Ct);
        var page = await client.GetAsync("/swagger/index.html", Ct);
        var config = await client.GetStringAsync("/swagger/index.js", Ct);

        Assert.Equal(HttpStatusCode.MovedPermanently, redirect.StatusCode);
        Assert.Equal("swagger/index.html", redirect.Headers.Location?.OriginalString);   // relativa: respeta prefijos
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("text/html", page.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"url\":\"../openapi/v1.json\"", config, StringComparison.Ordinal);
        Assert.Contains($"\"name\":\"{ServiceName} v1\"", config, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_documento_OpenAPI_es_valido_y_describe_el_servicio()
    {
        var client = factory.CreateClient();

        await using var stream = await client.GetStreamAsync("/openapi/v1.json", Ct);
        var result = await OpenApiDocument.LoadAsync(stream, cancellationToken: Ct);

        Assert.Empty(result.Diagnostic?.Errors ?? []);
        var document = Assert.IsType<OpenApiDocument>(result.Document);
        Assert.Equal($"SocialManagement · {ServiceName}", document.Info.Title);
        Assert.False(string.IsNullOrWhiteSpace(document.Info.Description));
        Assert.StartsWith("0.1.0", document.Info.Version, StringComparison.Ordinal);

        var version = document.Paths["/version"]!.Operations![HttpMethod.Get];
        Assert.Contains(version.Tags!, tag => tag.Name == "Version");
        Assert.False(string.IsNullOrWhiteSpace(version.Summary));          // proviene del comentario XML
        Assert.True(version.Responses!.ContainsKey("200"));
        Assert.Contains("VersionResponse", document.Components!.Schemas!.Keys);

        // Sin autenticación configurada no se anuncia ningún esquema de seguridad.
        Assert.True(document.Components.SecuritySchemes is null || document.Components.SecuritySchemes.Count == 0);
    }

    [Fact]
    public async Task Detras_del_Gateway_el_documento_anuncia_la_URL_publica_con_HTTPS_y_prefijo()
    {
        using var behindProxy = WithRemoteIp(factory, IPAddress.Loopback);   // proxy de confianza
        var client = behindProxy.CreateClient();

        var servers = await GetServersAsync(client, ForwardedRequest());

        Assert.Equal($"https://api.midominio.test{GatewayPrefix}", Assert.Single(servers).TrimEnd('/'));
    }

    [Fact]
    public async Task Los_encabezados_X_Forwarded_de_un_proxy_no_confiable_se_ignoran()
    {
        using var untrusted = WithRemoteIp(factory, IPAddress.Parse("203.0.113.10"));   // IP pública de documentación
        var client = untrusted.CreateClient();

        var servers = await GetServersAsync(client, ForwardedRequest());

        Assert.DoesNotContain(servers, url => url.Contains("midominio", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Con_un_esquema_Bearer_aparece_Authorize_solo_en_los_endpoints_protegidos()
    {
        using var withBearer = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication().AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestBearerHandler>("Bearer", _ => { });
            services.AddControllers().AddApplicationPart(typeof(ProtectedTestController).Assembly);
        }));
        var client = withBearer.CreateClient();

        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", Ct));
        var root = json.RootElement;

        var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

        var protectedOperation = root.GetProperty("paths").GetProperty("/pruebas/protegido").GetProperty("get");
        Assert.True(protectedOperation.TryGetProperty("security", out _));
        Assert.True(protectedOperation.GetProperty("responses").TryGetProperty("401", out _));

        var publicOperation = root.GetProperty("paths").GetProperty("/version").GetProperty("get");
        Assert.False(publicOperation.TryGetProperty("security", out _));
    }

    // ---------------------------------------------------------------- Configuración por ambiente

    [Fact]
    public async Task Deshabilitada_no_publica_Swagger_ni_el_documento_y_los_endpoints_siguen_funcionando()
    {
        using var disabled = new CustomBeneficiariosApiFactory(new() { ["ApiDocumentation:Enabled"] = "false" }, "Development");
        var client = disabled.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/version", Ct)).StatusCode);
    }

    [Fact]
    public async Task En_Production_esta_deshabilitada_por_defecto()
    {
        using var production = new CustomBeneficiariosApiFactory([], "Production");
        var client = production.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json", Ct)).StatusCode);
    }

    [Fact]
    public void En_Production_no_arranca_si_se_habilita_sin_control_de_acceso()
    {
        using var production = new CustomBeneficiariosApiFactory(new() { ["ApiDocumentation:Enabled"] = "true" }, "Production");

        var exception = Assert.ThrowsAny<Exception>(() => production.CreateClient());

        Assert.Contains("ApiDocumentation__Access__UserName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task En_Staging_esta_habilitada()
    {
        using var staging = new CustomBeneficiariosApiFactory([], "Staging");
        var client = staging.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("swagger/")]
    [InlineData("openapi")]
    [InlineData("Swagger")]
    public void Una_ruta_de_Swagger_invalida_impide_el_arranque(string routePrefix)
    {
        using var invalid = new CustomBeneficiariosApiFactory(new() { ["ApiDocumentation:RoutePrefix"] = routePrefix }, "Development");

        var exception = Assert.ThrowsAny<Exception>(() => invalid.CreateClient());

        Assert.Contains("ApiDocumentation:RoutePrefix", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task La_ruta_de_Swagger_es_configurable()
    {
        using var custom = new CustomBeneficiariosApiFactory(new() { ["ApiDocumentation:RoutePrefix"] = "docs/api" }, "Development");
        var client = custom.CreateClient();

        var page = await client.GetAsync("/docs/api/index.html", Ct);
        var config = await client.GetStringAsync("/docs/api/index.js", Ct);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("\"url\":\"../../openapi/v1.json\"", config, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- Control de acceso

    [Fact]
    public async Task Con_credenciales_exige_autenticacion_Basic_solo_en_la_documentacion()
    {
        using var production = ProductionWithAccess();
        var client = production.CreateClient();

        var anonymous = await client.GetAsync("/swagger/index.html", Ct);
        var anonymousJson = await client.GetAsync("/openapi/v1.json", Ct);
        var publicEndpoint = await client.GetAsync("/version", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("Basic", anonymous.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousJson.StatusCode);
        Assert.Equal(HttpStatusCode.OK, publicEndpoint.StatusCode);
    }

    [Theory]
    [InlineData(DocsUser, "clave-incorrecta")]
    [InlineData("otro-usuario", DocsPassword)]
    [InlineData("", "")]
    public async Task Rechaza_credenciales_incorrectas(string user, string password)
    {
        using var production = ProductionWithAccess();
        var client = production.CreateClient();
        client.DefaultRequestHeaders.Authorization = Basic(user, password);

        var response = await client.GetAsync("/openapi/v1.json", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Acepta_las_credenciales_correctas()
    {
        using var production = ProductionWithAccess();
        var client = production.CreateClient();
        client.DefaultRequestHeaders.Authorization = Basic(DocsUser, DocsPassword);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json", Ct)).StatusCode);
    }

    [Fact]
    public void Una_clave_corta_impide_el_arranque()
    {
        using var weak = new CustomBeneficiariosApiFactory(
            new() { ["ApiDocumentation:Access:UserName"] = DocsUser, ["ApiDocumentation:Access:Password"] = "corta" },
            "Development");

        var exception = Assert.ThrowsAny<Exception>(() => weak.CreateClient());

        Assert.Contains("ApiDocumentation:Access:Password", exception.Message, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- Auxiliares

    private static CustomBeneficiariosApiFactory ProductionWithAccess() => new(
        new()
        {
            ["ApiDocumentation:Enabled"] = "true",
            ["ApiDocumentation:Access:UserName"] = DocsUser,
            ["ApiDocumentation:Access:Password"] = DocsPassword,
        },
        "Production");

    private static WebApplicationFactory<Program> WithRemoteIp(WebApplicationFactory<Program> source, IPAddress ip) =>
        source.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(ip))));

    private static HttpRequestMessage ForwardedRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-Host", "api.midominio.test");
        request.Headers.Add("X-Forwarded-Prefix", GatewayPrefix);
        return request;
    }

    private static async Task<List<string>> GetServersAsync(HttpClient client, HttpRequestMessage request)
    {
        using var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return json.RootElement.GetProperty("servers").EnumerateArray().Select(s => s.GetProperty("url").GetString()!).ToList();
    }

    private static AuthenticationHeaderValue Basic(string user, string password) =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
}
