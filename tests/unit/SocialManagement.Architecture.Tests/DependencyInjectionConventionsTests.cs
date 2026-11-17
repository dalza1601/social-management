namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Cada capa de Clean Architecture que registra servicios tiene su propio <c>DependencyInjection/DependencyInjection.cs</c>
/// (carpeta DependencyInjection), con una clase estática <c>DependencyInjection</c> en el namespace de la capa y un
/// único método de registro:
/// <code>
/// Application    → AddApplication(this IServiceCollection services)
/// Infrastructure → AddInfrastructure(this IServiceCollection services)
/// Api            → AddPresentation(this IServiceCollection services)
/// </code>
/// Domain no tiene: no depende de ningún paquete (ni siquiera de las abstracciones de DI) ni registra servicios.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DependencyInjectionConventionsTests
{
    public static TheoryData<string, string, string> Layers => new()
    {
        { "Beneficiarios", "Application", "AddApplication" },
        { "Beneficiarios", "Infrastructure", "AddInfrastructure" },
        { "Beneficiarios", "Api", "AddPresentation" },
        { "Solicitudes", "Application", "AddApplication" },
        { "Solicitudes", "Infrastructure", "AddInfrastructure" },
        { "Solicitudes", "Api", "AddPresentation" },
    };

    public static TheoryData<string> Services => new() { "Beneficiarios", "Solicitudes" };

    [Theory]
    [MemberData(nameof(Layers))]
    public void Cada_capa_tiene_su_DependencyInjection_con_un_metodo_de_registro(string service, string layer, string method)
    {
        var project = Path.Combine(RepositoryPaths.ServiceDirectory(service), $"SocialManagement.{service}.{layer}");
        var file = Path.Combine(project, "DependencyInjection", "DependencyInjection.cs");

        Assert.False(File.Exists(Path.Combine(project, "DependencyInjection.cs")), "DependencyInjection.cs va dentro de la carpeta DependencyInjection/, no en la raíz.");

        Assert.True(File.Exists(file), $"Falta {Path.GetRelativePath(RepositoryPaths.Root, file)}.");
        var code = File.ReadAllText(file);
        Assert.Contains($"namespace SocialManagement.{service}.{layer};", code, StringComparison.Ordinal);
        Assert.Contains("public static class DependencyInjection", code, StringComparison.Ordinal);
        Assert.Contains($"public static IServiceCollection {method}(this IServiceCollection services)", code, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Program_compone_las_tres_capas_en_orden(string service)
    {
        var program = File.ReadAllText(Path.Combine(
            RepositoryPaths.ServiceDirectory(service), $"SocialManagement.{service}.Api", "Program.cs"));
        var normalized = string.Concat(program.Where(c => !char.IsWhiteSpace(c)));

        Assert.Contains(".AddApplication().AddInfrastructure().AddPresentation();", normalized, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_no_tiene_registro_de_dependencias(string service)
    {
        var domain = Path.Combine(RepositoryPaths.ServiceDirectory(service), $"SocialManagement.{service}.Domain");

        var sourceFiles = Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        Assert.DoesNotContain(sourceFiles, file => Path.GetFileName(file).StartsWith("DependencyInjection", StringComparison.Ordinal));
    }
}
