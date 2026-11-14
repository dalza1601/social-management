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
