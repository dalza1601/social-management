using System.Text.RegularExpressions;

namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Convenciones de organización del código fuente (src y tests):
/// - Cada archivo declara UN solo tipo de primer nivel (clase, record, interfaz, enum o struct).
/// - El archivo se llama igual que el tipo que declara.
/// Así cada componente se encuentra y se referencia por su nombre, sin buscar dentro de otros archivos.
/// Los tipos anidados (dentro de otra clase) están permitidos.
/// </summary>
[Trait("Category", "Unit")]
public sealed partial class SourceFileConventionsTests
{
    public static TheoryData<string> SourceRoots => new() { "src", "tests" };

    [Theory]
    [MemberData(nameof(SourceRoots))]
    public void Cada_archivo_declara_un_solo_tipo_de_primer_nivel(string root)
    {
        var violations = SourceFiles(root)
            .Select(file => (File: file, Types: TopLevelTypes(file)))
            .Where(entry => entry.Types.Count > 1)
            .Select(entry => $"{Relative(entry.File)} declara {entry.Types.Count} tipos: {string.Join(", ", entry.Types)}")
            .ToList();

        Assert.True(violations.Count == 0, "Mueve cada tipo a su propio archivo:\n" + string.Join('\n', violations));
    }

    [Theory]
    [MemberData(nameof(SourceRoots))]
    public void El_archivo_se_llama_igual_que_su_tipo(string root)
    {
        var violations = SourceFiles(root)
            .Select(file => (File: file, Types: TopLevelTypes(file)))
            .Where(entry => entry.Types.Count == 1 && entry.Types[0] != Path.GetFileNameWithoutExtension(entry.File))
            .Select(entry => $"{Relative(entry.File)} declara {entry.Types[0]}")
            .ToList();

        Assert.True(violations.Count == 0, "Renombra el archivo como su tipo:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void La_regla_detecta_tipos_reales()
    {
        // Evita que las pruebas anteriores pasen "en vacío" si cambia el formato del código.
        var file = Path.Combine(RepositoryPaths.Root, "src", "backend", "services", "beneficiarios",
            "SocialManagement.Beneficiarios.Api", "Contracts", "Diagnostics", "DemoStatusResponse.cs");

        Assert.Equal(["DemoStatusResponse"], TopLevelTypes(file));
    }

    private static IEnumerable<string> SourceFiles(string root) =>
        Directory.EnumerateFiles(Path.Combine(RepositoryPaths.Root, root), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file))
            .Where(file => Path.GetFileName(file) != "Program.cs");   // top-level statements: sin tipo propio

    private static bool IsBuildOutput(string file) =>
        file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj" or "node_modules");

    // Los namespaces son de archivo (file-scoped): los tipos de primer nivel empiezan en la columna 0.
    private static List<string> TopLevelTypes(string file) =>
        File.ReadLines(file)
            .Select(line => TopLevelTypeDeclaration().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .ToList();

    private static string Relative(string file) => Path.GetRelativePath(RepositoryPaths.Root, file);

    [GeneratedRegex(@"^(?:public |internal |file )?(?:(?:sealed|static|abstract|partial|readonly) )*(?:class|record|interface|enum|struct) (?<name>\w+)")]
    private static partial Regex TopLevelTypeDeclaration();
}
