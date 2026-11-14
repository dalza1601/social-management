namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>Un servicio de docker-compose.yml y acceso tipado a su configuración.</summary>
internal sealed record ComposeService(string Name, Dictionary<object, object> Node)
{
    public IReadOnlyList<string> Profiles => ComposeDocument.List(Node, "profiles");

    public IReadOnlyList<string> Ports => ComposeDocument.List(Node, "ports");

    public IReadOnlyList<string> Networks => ComposeDocument.List(Node, "networks");

    public IReadOnlyList<string> Volumes => ComposeDocument.List(Node, "volumes");

    public bool HasHealthCheck => Node.ContainsKey("healthcheck");

    public IReadOnlyDictionary<string, string> Environment =>
        ComposeDocument.Map(Node, "environment").ToDictionary(e => e.Key.ToString()!, e => e.Value?.ToString() ?? string.Empty);

    public string? DependencyCondition(string service) =>
        ComposeDocument.Map(Node, "depends_on").TryGetValue(service, out var value) && value is Dictionary<object, object> map
            ? map.GetValueOrDefault("condition")?.ToString()
            : null;
}
