namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Reglas de dependencia verificadas sobre los .csproj (lo que el compilador permite referenciar).
///   Domain         -> (nada)
///   Application    -> Domain
///   Infrastructure -> Application, Domain
///   Api            -> Application, Infrastructure
/// </summary>
[Trait("Category", "Unit")]
public sealed class ProjectReferenceRulesTests
{
    public static TheoryData<string> Services => new() { "Beneficiarios", "Solicitudes" };

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_no_depende_de_ningun_proyecto_ni_paquete(string service)
    {
        var project = ProjectFile.Load(RepositoryPaths.ProjectFile(service, "Domain"));

        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.PackageReferences);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_solo_depende_de_Domain(string service)
    {
        var project = ProjectFile.Load(RepositoryPaths.ProjectFile(service, "Application"));

        Assert.Equal([Name(service, "Domain")], project.ProjectReferences);
        Assert.All(project.PackageReferences, package =>
            Assert.Contains(package, AllowedApplicationPackages));
    }

    /// <summary>
    /// Paquetes permitidos en Application: ninguno es infraestructura, HTTP ni persistencia.
    /// - Abstracción de DI (solo interfaces).
    /// - FluentValidation: validación de los datos de entrada de los casos de uso.
    /// - AutoMapper: mapeo de entidades a DTOs (sin HTTP ni persistencia).
    /// </summary>
    private static readonly string[] AllowedApplicationPackages =
    [
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "FluentValidation",
        "FluentValidation.DependencyInjectionExtensions",
        "AutoMapper",
    ];

    [Theory]
    [MemberData(nameof(Services))]
    public void Infrastructure_depende_de_Application_y_Domain(string service)
    {
        var project = ProjectFile.Load(RepositoryPaths.ProjectFile(service, "Infrastructure"));

        Assert.Equal(
            new[] { Name(service, "Application"), Name(service, "Domain") }.Order(),
            project.ProjectReferences.Order());
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Api_depende_de_Application_e_Infrastructure(string service)
    {
        var project = ProjectFile.Load(RepositoryPaths.ProjectFile(service, "Api"));

        Assert.Equal(
            new[] { Name(service, "Application"), Name(service, "Infrastructure") }.Order(),
            project.ProjectReferences.Order());
    }

    [Theory]
    [InlineData("Beneficiarios", "Solicitudes")]
    [InlineData("Solicitudes", "Beneficiarios")]
    public void Un_microservicio_no_referencia_proyectos_de_otro(string service, string otherService)
    {
        string[] layers = ["Domain", "Application", "Infrastructure", "Api"];

        foreach (var layer in layers)
        {
            var project = ProjectFile.Load(RepositoryPaths.ProjectFile(service, layer));
            Assert.DoesNotContain(project.ProjectReferences, reference => reference.Contains($".{otherService}.", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void No_existe_una_biblioteca_compartida_de_dominio_entre_microservicios()
    {
        var backend = Path.Combine(RepositoryPaths.Root, "src", "backend");
        var sharedDomainProjects = Directory
            .EnumerateFiles(backend, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name!.Contains("Shared", StringComparison.OrdinalIgnoreCase)
                           || name.Contains("Common", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(sharedDomainProjects);
    }

    private static string Name(string service, string layer) => $"SocialManagement.{service}.{layer}";
}
