namespace SocialManagement.Architecture.Tests;

/// <summary>Localiza la raíz del repositorio (la carpeta que contiene SocialManagement.slnx).</summary>
internal static class RepositoryPaths
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    public static string Root => RootPath.Value;

    public static string ServiceDirectory(string service) =>
        Path.Combine(Root, "src", "backend", "services", service.ToLowerInvariant());

    public static string ProjectFile(string service, string layer)
    {
        var name = $"SocialManagement.{service}.{layer}";
        return Path.Combine(ServiceDirectory(service), name, $"{name}.csproj");
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
