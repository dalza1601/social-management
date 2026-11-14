using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Formato uniforme ApiResponse de los controllers que heredan de BaseApiController, a través de todo el
/// pipeline HTTP (routing, validación del modelo, manejo global de excepciones y OpenAPI).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ApiResponseFormatTests : IClassFixture<BeneficiariosApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiResponseFormatTests(BeneficiariosApiFactory factory) =>
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(EnvelopeTestController).Assembly)));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Exito_200_con_los_datos_dentro_de_data()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/envelopetest/1", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        var root = body.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").GetProperty("id").GetInt32());
        Assert.Equal("Elemento de prueba", root.GetProperty("data").GetProperty("nombre").GetString());
        Assert.Equal(0, root.GetProperty("errors").GetArrayLength());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task No_encontrado_404_con_el_mismo_formato()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/envelopetest/2", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("No existe el elemento 2.", body.RootElement.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("data").ValueKind);
    }

    [Fact]
    public async Task Creado_201_con_Location_en_minusculas()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/envelopetest", new { nombre = "Nuevo", cantidad = 3 }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith("/api/v1/envelopetest/1", response.Headers.Location?.ToString(), StringComparison.Ordinal);
        using var body = await ReadJsonAsync(response);
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Elemento creado.", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Validacion_400_lista_los_campos_con_error()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/envelopetest", new { }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = await ReadJsonAsync(response);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("La solicitud contiene datos no válidos.", body.RootElement.GetProperty("message").GetString());

        var errors = body.RootElement.GetProperty("errors").EnumerateArray()
            .ToDictionary(e => e.GetProperty("field").GetString()!, e => e.GetProperty("message").GetString());
        Assert.Equal("El nombre es obligatorio.", errors["Nombre"]);
        Assert.Equal("La cantidad debe estar entre 1 y 10.", errors["Cantidad"]);
    }

    [Fact]
    public async Task Regla_de_dominio_422_con_el_mismo_formato()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/envelopetest/regla-de-dominio", Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = await ReadJsonAsync(response);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Regla de dominio incumplida", body.RootElement.GetProperty("message").GetString());
        Assert.Equal("La regla de prueba no se cumple.", body.RootElement.GetProperty("errors")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Error_inesperado_500_sin_exponer_detalles_internos()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/envelopetest/error-inesperado", Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("\"success\":false", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secreto", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validacion_de_FluentValidation_400_con_un_error_por_campo()
    {
        var response = await _factory.CreateClient().PostAsync("/api/v1/envelopetest/validacion-de-caso-de-uso", null, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("La solicitud contiene datos no válidos.", body.RootElement.GetProperty("message").GetString());

        var errors = body.RootElement.GetProperty("errors").EnumerateArray()
            .ToDictionary(e => e.GetProperty("field").GetString()!, e => e.GetProperty("message").GetString());
        Assert.Equal("Los nombres son obligatorios.", errors["Nombres"]);
        Assert.Equal("El DNI debe tener exactamente 8 dígitos.", errors["NumeroDocumento"]);
    }

    [Fact]
    public async Task Las_rutas_de_los_controllers_incluyen_la_version_y_la_informan()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/envelopetest/1", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("1.0", response.Headers.GetValues("api-supported-versions").Single(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/v9/envelopetest/1")]     // versión inexistente
    [InlineData("/api/envelopetest/1")]        // sin versión en la URL
    public async Task Una_version_inexistente_o_ausente_no_llega_al_controller(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url, Ct);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"{url} respondió {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task Los_Minimal_APIs_y_las_rutas_inexistentes_siguen_con_ProblemDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/no-existe", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenAPI_documenta_las_respuestas_de_error_comunes_de_los_controllers()
    {
        using var document = JsonDocument.Parse(await _factory.CreateClient().GetStringAsync("/openapi/v1.json", Ct));

        var responses = document.RootElement.GetProperty("paths").GetProperty("/api/v1/envelopetest/{id}")
            .GetProperty("get").GetProperty("responses");
        foreach (var code in new[] { "200", "400", "422", "500" })
        {
            Assert.True(responses.TryGetProperty(code, out _), $"Falta la respuesta {code} en OpenAPI.");
        }
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
}
