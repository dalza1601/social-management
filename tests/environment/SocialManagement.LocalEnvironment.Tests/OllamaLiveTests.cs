namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>Servidor Ollama opcional. Se omite salvo que SM_OLLAMA_TESTS=true.</summary>
[Trait("Category", "Environment")]
public sealed class OllamaLiveTests
{
    public OllamaLiveTests() => Assert.SkipUnless(
        LocalEnvironment.OllamaTestsEnabled,
        "Requiere Ollama en ejecución (perfil 'ollama'). Actívalo con SM_OLLAMA_TESTS=true.");

    [Fact]
    public async Task Ollama_responde_en_su_endpoint()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var port = LocalEnvironment.GetPort("OLLAMA_PORT", 11434);

        var version = await http.GetStringAsync($"http://localhost:{port}/api/version", TestContext.Current.CancellationToken);

        Assert.Contains("version", version, StringComparison.Ordinal);
    }
}
