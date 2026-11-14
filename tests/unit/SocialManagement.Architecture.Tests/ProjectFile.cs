using System.Xml.Linq;

namespace SocialManagement.Architecture.Tests;

/// <summary>Lectura mínima de un .csproj: referencias a proyectos y a paquetes NuGet.</summary>
internal sealed class ProjectFile
{
    private ProjectFile(IReadOnlyList<string> projectReferences, IReadOnlyList<string> packageReferences)
    {
        ProjectReferences = projectReferences;
        PackageReferences = packageReferences;
    }

    /// <summary>Nombres de los proyectos referenciados (sin ruta ni extensión).</summary>
    public IReadOnlyList<string> ProjectReferences { get; }

    public IReadOnlyList<string> PackageReferences { get; }

    public static ProjectFile Load(string path)
    {
        Assert.True(File.Exists(path), $"No existe el proyecto {path}");
        var document = XDocument.Load(path);

        static IReadOnlyList<string> Includes(XDocument doc, string element) =>
            doc.Descendants(element)
                .Select(e => (string?)e.Attribute("Include"))
                .OfType<string>()
                .ToList();

        var projects = Includes(document, "ProjectReference")
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .ToList();

        return new ProjectFile(projects, Includes(document, "PackageReference"));
    }
}
