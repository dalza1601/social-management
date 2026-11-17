using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using SocialManagement.Beneficiarios.Api.Contracts.Authentication;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// POST /api/auth/dev-token: emisión de tokens SOLO para desarrollo, con usuarios demo configurados fuera del código.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DevelopmentTokenEndpointTests(BeneficiariosApiFactory factory) : IClassFixture<BeneficiariosApiFactory>
{
    private const string Ruta = "/api/auth/dev-token";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Con_credenciales_correctas_emite_un_token_firmado_con_los_roles_del_usuario()
    {
        using var response = await PedirTokenAsync(factory, TestTokens.AdminUser, TestTokens.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<DevTokenResponse>(Ct))!;
        Assert.Equal("Bearer", body.TokenType);
        Assert.Equal(3600, body.ExpiresIn);   // Jwt:ExpirationMinutes = 60
        Assert.Equal(TestTokens.AdminUser, body.UserName);
        Assert.Equal(["Administrador"], body.Roles);

        // Un JWT se puede LEER sin la clave (no está cifrado): header.payload.firma en Base64Url.
        var jwt = new JsonWebToken(body.AccessToken);
        Assert.Equal("HS256", jwt.Alg);
        Assert.Equal(TestTokens.Issuer, jwt.Issuer);
        Assert.Equal(TestTokens.Audience, Assert.Single(jwt.Audiences));
        Assert.Equal(TestTokens.AdminUser, jwt.Subject);
        Assert.Equal("Administrador", jwt.GetClaim("role").Value);
        Assert.Equal(TimeSpan.FromHours(1), jwt.ValidTo - jwt.IssuedAt);
        Assert.DoesNotContain(TestTokens.AdminPassword, jwt.EncodedPayload, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TestTokens.AdminUser, "clave-incorrecta")]
    [InlineData("usuario.inexistente", TestTokens.AdminPassword)]
    [InlineData(TestTokens.OperadorUser, TestTokens.AdminPassword)]
    public async Task Credenciales_incorrectas_responden_401_sin_revelar_que_fallo(string userName, string password)
    {
        using var response = await PedirTokenAsync(factory, userName, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal("Credenciales no válidas.", problem.RootElement.GetProperty("title").GetString());
        Assert.False(problem.RootElement.TryGetProperty("accessToken", out _));
    }

    [Fact]
    public async Task Usuario_y_clave_vacios_responden_400_con_los_errores_por_campo()
    {
        using var response = await PedirTokenAsync(factory, userName: "", password: " ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("userName", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task El_token_emitido_da_acceso_segun_el_rol()
    {
        var operador = await ObtenerTokenAsync(TestTokens.OperadorUser, TestTokens.OperadorPassword);
        var admin = await ObtenerTokenAsync(TestTokens.AdminUser, TestTokens.AdminPassword);
        var client = factory.CreateClient();

        // Id 0: si la petición pasa la seguridad, el validador responde 400 (no hace falta SQL Server).
        using var operadorLee = await EnviarAsync(client, HttpMethod.Get, "/api/v1/beneficiarios/0", operador);
        using var operadorDesactiva = await EnviarAsync(client, HttpMethod.Patch, "/api/v1/beneficiarios/0/desactivar", operador);
        using var adminDesactiva = await EnviarAsync(client, HttpMethod.Patch, "/api/v1/beneficiarios/0/desactivar", admin);

        Assert.Equal(HttpStatusCode.BadRequest, operadorLee.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, operadorDesactiva.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, adminDesactiva.StatusCode);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Fuera_de_Development_la_ruta_no_existe(string environment)
    {
        using var api = new CustomBeneficiariosApiFactory([], environment);

        using var response = await PedirTokenAsync(api, TestTokens.AdminUser, TestTokens.AdminPassword);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Activarla_fuera_de_Development_impide_el_arranque(string environment)
    {
        using var api = new CustomBeneficiariosApiFactory(new() { ["DevelopmentTokens:Enabled"] = "true" }, environment);

        var exception = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("DevelopmentTokens:Enabled", exception.Message, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- Auxiliares

    private static async Task<HttpResponseMessage> PedirTokenAsync(BeneficiariosApiFactory api, string userName, string password) =>
        await api.CreateClient().PostAsJsonAsync(Ruta, new { userName, password }, Ct);

    private async Task<string> ObtenerTokenAsync(string userName, string password)
    {
        using var response = await PedirTokenAsync(factory, userName, password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DevTokenResponse>(Ct))!.AccessToken;
    }

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient client, HttpMethod method, string url, string token)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = TestTokens.Bearer(token);
        return await client.SendAsync(request, Ct);
    }
}
