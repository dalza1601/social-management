namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>
/// Rutas del repositorio y valores de .env (o .env.example si .env no existe).
/// Las variables de entorno del proceso tienen prioridad sobre el archivo.
/// </summary>
internal static class LocalEnvironment
{
    private static readonly Lazy<string> RootPath = new(FindRoot);
    private static readonly Lazy<IReadOnlyDictionary<string, string>> DotEnv = new(LoadDotEnv);

    public static string Root => RootPath.Value;

    public static string ComposeFile => Path.Combine(Root, "infra", "docker", "docker-compose.yml");

    public static string EnvExampleFile => Path.Combine(Root, ".env.example");

    public static bool LiveTestsEnabled => IsEnabled("SM_ENVIRONMENT_TESTS");

    public static bool LocalStackTestsEnabled => IsEnabled("SM_LOCALSTACK_TESTS");

    public static bool OllamaTestsEnabled => IsEnabled("SM_OLLAMA_TESTS");

    public static string Get(string key, string defaultValue = "")
    {
        var fromProcess = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(fromProcess))
        {
            return fromProcess;
        }

        return DotEnv.Value.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    public static int GetPort(string key, int defaultValue) => int.TryParse(Get(key), out var port) ? port : defaultValue;

    public static IReadOnlyDictionary<string, string> ParseDotEnv(string path) =>
        File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#') && line.Contains('='))
            .Select(line => line.Split('=', 2))
            .GroupBy(parts => parts[0].Trim())
            .ToDictionary(group => group.Key, group => group.Last()[1].Trim());

    private static bool IsEnabled(string variable) =>
        string.Equals(Environment.GetEnvironmentVariable(variable), "true", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string> LoadDotEnv()
    {
        var envFile = Path.Combine(Root, ".env");
        return ParseDotEnv(File.Exists(envFile) ? envFile : EnvExampleFile);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SocialManagement.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new InvalidOperationException("No se encontró SocialManagement.slnx en ningún directorio superior.");
    }
}
