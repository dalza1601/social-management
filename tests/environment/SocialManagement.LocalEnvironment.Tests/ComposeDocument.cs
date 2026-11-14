using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>Lectura de docker-compose.yml resolviendo anclas y claves de fusión (&lt;&lt;: *ancla).</summary>
internal sealed class ComposeDocument
{
    private readonly Dictionary<object, object> _root;

    private ComposeDocument(Dictionary<object, object> root) => _root = root;

    public static ComposeDocument Load(string path)
    {
        using var reader = new StringReader(File.ReadAllText(path));
        var parser = new MergingParser(new Parser(reader));
        var root = new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>>(parser);
        return new ComposeDocument(root);
    }

    public IReadOnlyDictionary<string, ComposeService> Services =>
        Map(_root, "services").ToDictionary(
            entry => entry.Key.ToString()!,
            entry => new ComposeService(entry.Key.ToString()!, (Dictionary<object, object>)entry.Value));

    public IReadOnlyCollection<string> Networks => Map(_root, "networks").Keys.Select(k => k.ToString()!).ToList();

    public IReadOnlyCollection<string> Volumes => Map(_root, "volumes").Keys.Select(k => k.ToString()!).ToList();

    internal static Dictionary<object, object> Map(Dictionary<object, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is Dictionary<object, object> map ? map : [];

    internal static IReadOnlyList<string> List(Dictionary<object, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is List<object> list ? list.Select(i => i.ToString()!).ToList() : [];
}

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
