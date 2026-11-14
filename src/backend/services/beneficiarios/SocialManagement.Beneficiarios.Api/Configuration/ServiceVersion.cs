using System.Reflection;

namespace SocialManagement.Beneficiarios.Api.Configuration;

/// <summary>
/// Versión del servicio en ejecución, leída una sola vez del ensamblado de la API.
/// La define la propiedad &lt;Version&gt; de Directory.Build.props.
/// </summary>
public static class ServiceVersion
{
    public static string Current { get; } = Resolve(typeof(ServiceVersion).Assembly);

    internal static string Resolve(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "desconocida";
}
