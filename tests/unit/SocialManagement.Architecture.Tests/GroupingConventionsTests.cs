using System.Text.RegularExpressions;

namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Los contratos HTTP (Api/Contracts) y los value objects (Domain/ValueObjects) se agrupan en una subcarpeta por
/// entidad o funcionalidad, y su namespace coincide con la carpeta:
/// <code>
/// Api/Contracts/Beneficiarios/RegistrarBeneficiarioRequest.cs → SocialManagement.Beneficiarios.Api.Contracts.Beneficiarios
/// Api/Contracts/Authentication/DevTokenRequest.cs           → SocialManagement.Beneficiarios.Api.Contracts.Authentication
/// Api/Contracts/Common/ApiResponse.cs                       → SocialManagement.Beneficiarios.Api.Contracts.Common
/// Domain/ValueObjects/Beneficiarios/DocumentoIdentidad.cs   → SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios
/// </code>
/// </summary>
[Trait("Category", "Unit")]
public sealed partial class GroupingConventionsTests
{
    /// <summary>(servicio, capa, carpeta agrupada).</summary>
    public static TheoryData<string, string, string> GroupedFolders => new()
    {
        { "Beneficiarios", "Api", "Contracts" },
        { "Beneficiarios", "Domain", "ValueObjects" },
        { "Solicitudes", "Api", "Contracts" },
        { "Solicitudes", "Domain", "ValueObjects" },
    };

    [Theory]
    [MemberData(nameof(GroupedFolders))]
    public void No_hay_archivos_sueltos_fuera_de_una_carpeta_por_entidad(string service, string layer, string folder)
    {
        var root = FolderPath(service, layer, folder);
        if (!Directory.Exists(root))
        {
            return;   // p. ej. Solicitudes todavía no tiene value objects
        }

        var loose = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(loose.Count == 0, $"Mueve a {layer}/{folder}/<Entidad>/: {string.Join(", ", loose)}");
    }

    [Theory]
    [MemberData(nameof(GroupedFolders))]
    public void El_namespace_coincide_con_la_carpeta_de_la_entidad(string service, string layer, string folder)
    {
        var root = FolderPath(service, layer, folder);
        if (!Directory.Exists(root))
        {
            return;
        }

        var projectDirectory = Path.GetDirectoryName(root)!;
        var violations = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(file =>
            {
                var relativeFolder = Path.GetRelativePath(projectDirectory, Path.GetDirectoryName(file)!)
                    .Replace(Path.DirectorySeparatorChar, '.')
                    .Replace(Path.AltDirectorySeparatorChar, '.');
                var expected = $"SocialManagement.{service}.{layer}.{relativeFolder}";
                var actual = NamespaceDeclaration().Match(File.ReadAllText(file)).Groups["name"].Value;
                return (File: Path.GetFileName(file), Expected: expected, Actual: actual);
            })
            .Where(entry => entry.Actual != entry.Expected)
            .Select(entry => $"{entry.File}: namespace {entry.Actual}, se esperaba {entry.Expected}")
            .ToList();

        Assert.True(violations.Count == 0, string.Join('\n', violations));
    }

    [Fact]
    public void La_regla_revisa_archivos_reales()
    {
        // Evita que las pruebas anteriores pasen "en vacío" si cambian las rutas.
        var contracts = FolderPath("Beneficiarios", "Api", "Contracts");

        Assert.True(File.Exists(Path.Combine(contracts, "Beneficiarios", "RegistrarBeneficiarioRequest.cs")));
        Assert.True(File.Exists(Path.Combine(FolderPath("Beneficiarios", "Domain", "ValueObjects"), "Beneficiarios", "DocumentoIdentidad.cs")));
    }

    private static string FolderPath(string service, string layer, string folder) =>
        Path.Combine(RepositoryPaths.ServiceDirectory(service), $"SocialManagement.{service}.{layer}", folder);

    [GeneratedRegex(@"^namespace (?<name>[\w.]+);", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();
}
