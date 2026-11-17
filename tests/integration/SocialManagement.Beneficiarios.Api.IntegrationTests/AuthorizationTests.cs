using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SocialManagement.Beneficiarios.Api.Contracts.Common;
using SocialManagement.Beneficiarios.Api.Security;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Acceso autorizado y no autorizado a /api/v1/beneficiarios (sesión 04).
/// </summary>
/// <remarks>
/// Estas pruebas no usan SQL Server. Truco: las peticiones AUTORIZADAS llevan datos inválidos (Id 0, página 0,
/// cuerpo vacío), así que el validador responde 400 antes de llegar a la base de datos. Un 400 demuestra que la
/// petición SUPERÓ la autenticación y la autorización: si no, la respuesta habría sido 401 o 403.
/// El flujo completo con datos reales está en SqlServer.IntegrationTests (BeneficiariosAutorizacionTests).
/// </remarks>
[Trait("Category", "Integration")]
public sealed class AuthorizationTests(BeneficiariosApiFactory factory) : IClassFixture<BeneficiariosApiFactory>
{
    private const string Ruta = "/api/v1/beneficiarios";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Todas las operaciones de negocio, con datos que el validador rechaza (400).</summary>
    public static TheoryData<string, string> Operaciones => new()
    {
        { "GET", $"{Ruta}/0" },
        { "GET", $"{Ruta}?pageNumber=0" },
        { "POST", Ruta },
        { "PUT", $"{Ruta}/0" },
        { "PATCH", $"{Ruta}/0/desactivar" },
    };

    /// <summary>Operaciones permitidas al rol Operador (todas menos desactivar).</summary>
    public static TheoryData<string, string> OperacionesDelOperador => new()
    {
        { "GET", $"{Ruta}/0" },
        { "GET", $"{Ruta}?pageNumber=0" },
        { "POST", Ruta },
        { "PUT", $"{Ruta}/0" },
    };

    // ---------------------------------------------------------------- 401: sin token o token inválido

    [Theory]
    [MemberData(nameof(Operaciones))]
    public async Task Sin_token_responde_401_con_formato_ApiResponse(string method, string url)
    {
        using var response = await SendAsync(method, url, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Ct);
        Assert.False(body!.Success);
        Assert.Equal(ApiResponseAuthorizationResultHandler.UnauthorizedMessage, body.Message);
        Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
    }

    public static TheoryData<string, string> TokensInvalidos => new()
    {
        { "expirado", TestTokens.Crear(TestTokens.AdminUser, [Roles.Administrador], expiresAt: DateTimeOffset.UtcNow.AddMinutes(-10)) },
        { "firmado con otra clave", TestTokens.Crear(TestTokens.AdminUser, [Roles.Administrador], signingKey: "Otra-Clave-Que-La-API-No-Conoce-0123456789") },
        { "de otro emisor", TestTokens.Crear(TestTokens.AdminUser, [Roles.Administrador], issuer: "emisor-desconocido") },
        { "para otra audiencia", TestTokens.Crear(TestTokens.AdminUser, [Roles.Administrador], audience: "otra-api") },
        { "alterado", Alterar(TestTokens.Administrador()) },
        { "que no es un JWT", "esto-no-es-un-token" },
    };

    [Theory]
    [MemberData(nameof(TokensInvalidos))]
    public async Task Un_token_invalido_responde_401(string caso, string token)
    {
        using var response = await SendAsync("GET", $"{Ruta}/0", token);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"Token {caso}: se esperaba 401 y fue {(int)response.StatusCode}.");
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Contains("error=\"invalid_token\"", challenge.Parameter, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- 403: token válido, rol insuficiente

    [Fact]
    public async Task El_operador_no_puede_desactivar_y_recibe_403()
    {
        using var response = await SendAsync("PATCH", $"{Ruta}/0/desactivar", TestTokens.Operador());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Ct);
        Assert.False(body!.Success);
        Assert.Equal(ApiResponseAuthorizationResultHandler.ForbiddenMessage, body.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Auditor")]
    public async Task Un_usuario_sin_un_rol_valido_recibe_403_incluso_en_lectura(string? rol)
    {
        var token = TestTokens.Crear("usuario.sin.rol", rol is null ? [] : [rol]);

        using var response = await SendAsync("GET", $"{Ruta}?pageNumber=0", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------------------------------------------------------- Acceso autorizado

    [Theory]
    [MemberData(nameof(Operaciones))]
    public async Task El_administrador_puede_realizar_todas_las_operaciones(string method, string url)
    {
        using var response = await SendAsync(method, url, TestTokens.Administrador());

        AssertAutorizada(response);
    }

    [Theory]
    [MemberData(nameof(OperacionesDelOperador))]
    public async Task El_operador_puede_consultar_listar_registrar_y_actualizar(string method, string url)
    {
        using var response = await SendAsync(method, url, TestTokens.Operador());

        AssertAutorizada(response);
    }

    // ---------------------------------------------------------------- Endpoints públicos y cobertura

    [Theory]
    [InlineData("/health")]
    [InlineData("/version")]
    [InlineData("/api/demo/status")]
    public async Task Los_endpoints_tecnicos_siguen_siendo_publicos(string url)
    {
        using var response = await factory.CreateClient().GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Toda_ruta_de_negocio_declara_explicitamente_su_politica_de_autorizacion()
    {
        // Evita que un endpoint nuevo quede público por olvido: cada ruta /api/v{n}/... debe tener una política.
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("api/v", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
        {
            var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy);
            Assert.True(
                policies.Any(policy => !string.IsNullOrWhiteSpace(policy)),
                $"{endpoint.DisplayName} no declara [Authorize(Policy = ...)].");
        });
    }

    // ---------------------------------------------------------------- Auxiliares

    private async Task<HttpResponseMessage> SendAsync(string method, string url, string? token)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        if (token is not null)
        {
            request.Headers.Authorization = TestTokens.Bearer(token);
        }

        return await factory.CreateClient().SendAsync(request, Ct);
    }

    /// <summary>Pasó la autenticación y la autorización: el 400 lo da el validador, no la seguridad.</summary>
    private static void AssertAutorizada(HttpResponseMessage response)
    {
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Cambia un carácter de la firma: el contenido ya no coincide con la firma.</summary>
    private static string Alterar(string token)
    {
        var chars = token.ToCharArray();
        var last = chars.Length - 2;
        chars[last] = chars[last] == 'A' ? 'B' : 'A';
        return new string(chars);
    }
}
