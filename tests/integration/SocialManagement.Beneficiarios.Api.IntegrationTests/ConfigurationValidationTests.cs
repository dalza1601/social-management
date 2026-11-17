namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Si falta una configuración obligatoria, la API no debe arrancar y el error debe
/// indicar qué clave falta y cómo definirla.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConfigurationValidationTests
{
    [Theory]
    [InlineData("ConnectionStrings:BeneficiariosDb", "ConnectionStrings__BeneficiariosDb")]
    [InlineData("RabbitMq:Password", "RabbitMq__Password")]
    [InlineData("Jwt:SigningKey", "Jwt__SigningKey")]
    public void El_arranque_falla_con_un_mensaje_claro_si_falta_configuracion(string key, string environmentVariable)
    {
        using var factory = new MisconfiguredBeneficiariosApiFactory(new() { [key] = "" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        Assert.Contains(environmentVariable, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Una_clave_JWT_de_menos_de_32_bytes_impide_el_arranque()
    {
        using var factory = new MisconfiguredBeneficiariosApiFactory(new() { ["Jwt:SigningKey"] = "clave-corta-de-prueba" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Jwt:SigningKey", exception.Message, StringComparison.Ordinal);
        Assert.Contains("32 bytes", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("DevelopmentTokens:Users:0:UserName", "", "UserName")]
    [InlineData("DevelopmentTokens:Users:1:Password", "corta", "Password")]
    [InlineData("DevelopmentTokens:Users:1:Roles:0", "Auditor", "Roles")]
    [InlineData("DevelopmentTokens:Users:1:UserName", "admin.prueba", "repetidos")]
    public void Un_usuario_demo_mal_configurado_impide_el_arranque_en_Development(string key, string value, string expected)
    {
        using var factory = new MisconfiguredBeneficiariosApiFactory(new() { [key] = value });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("DevelopmentTokens", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalStack_exige_un_endpoint_valido()
    {
        using var factory = new MisconfiguredBeneficiariosApiFactory(new() { ["Aws:ServiceUrl"] = "no-es-una-url" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Aws:ServiceUrl", exception.Message, StringComparison.Ordinal);
    }
}
