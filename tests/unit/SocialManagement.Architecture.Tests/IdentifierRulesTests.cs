using System.Reflection;
using System.Text.RegularExpressions;

namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Regla del proyecto (desde el 14/11/2026): los identificadores son ENTEROS y no se usa GUID en ninguna parte.
/// - Las entidades se identifican con <c>int Id</c>, generado por SQL Server (columna IDENTITY).
/// - Ningún archivo de código fuente (src y tests) usa el tipo System.Guid.
/// </summary>
[Trait("Category", "Unit")]
public sealed partial class IdentifierRulesTests
{
    public static TheoryData<string> Services => new() { "Beneficiarios", "Solicitudes" };

    public static TheoryData<string> SourceRoots => new() { "src", "tests" };

    [Theory]
    [MemberData(nameof(Services))]
    public void Las_entidades_se_identifican_con_un_entero(string service)
    {
        var violations = Entities(service)
            .Select(entity => (Entity: entity, Id: entity.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)))
            .Where(entry => entry.Id is not null && entry.Id.PropertyType != typeof(int))
            .Select(entry => $"{entry.Entity.Name}.Id es {entry.Id!.PropertyType.Name}")
            .ToList();

        Assert.True(violations.Count == 0, "Usa int como Id:\n" + string.Join('\n', violations));
    }

    [Theory]
    [MemberData(nameof(SourceRoots))]
    public void Ningun_archivo_usa_el_tipo_Guid(string root)
    {
        var thisFile = $"{nameof(IdentifierRulesTests)}.cs";

        var violations = Directory.EnumerateFiles(Path.Combine(RepositoryPaths.Root, root), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file) && Path.GetFileName(file) != thisFile)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => (File: file, Line: index + 1, Text: line))
                .Where(entry => GuidType().IsMatch(entry.Text)))
            .Select(entry => $"{Path.GetRelativePath(RepositoryPaths.Root, entry.File)}:{entry.Line}: {entry.Text.Trim()}")
            .ToList();

        Assert.True(violations.Count == 0, "El proyecto no usa GUID (usa identificadores int):\n" + string.Join('\n', violations));
    }

    [Fact]
    public void La_regla_de_identificadores_inspecciona_entidades_reales()
    {
        // Evita que la regla pase "en vacío": Beneficiario debe estar entre las entidades revisadas.
        Assert.Contains(Entities("Beneficiarios"), entity => entity.Name == "Beneficiario");
    }

    // Uso del TIPO (Guid, System.Guid, Guid.NewGuid...). Coincide con mayúsculas exactas para no marcar
    // textos como "GUID" en comentarios.
    [GeneratedRegex(@"\bGuid\b")]
    private static partial Regex GuidType();

    private static IEnumerable<Type> Entities(string service) =>
        Assembly.Load(new AssemblyName($"SocialManagement.{service}.Domain")).GetTypes()
            .Where(t => t.Namespace == $"SocialManagement.{service}.Domain.Entities"
                        && t is { IsClass: true, IsAbstract: false, IsNested: false }
                        && !t.Name.StartsWith('<'));

    private static bool IsBuildOutput(string file) =>
        file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj" or "node_modules");
}
