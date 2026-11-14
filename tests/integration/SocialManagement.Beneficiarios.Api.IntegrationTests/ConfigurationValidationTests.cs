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
    public void El_arranque_falla_con_un_mensaje_claro_si_falta_configuracion(string key, string environmentVariable)
    {
        using var factory = new MisconfiguredBeneficiariosApiFactory(new() { [key] = "" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        Assert.Contains(environmentVariable, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalStack_exige_un_endpoint_valido()
    {
        using var factory = new MisconfiguredBeneficiariosApiFactory(new() { ["Aws:ServiceUrl"] = "no-es-una-url" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Aws:ServiceUrl", exception.Message, StringComparison.Ordinal);
    }
}
