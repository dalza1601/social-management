using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

// Vive en la carpeta DependencyInjection/, pero su namespace es el de la capa (no "...DependencyInjection"): así Program.cs
// no necesita otro using y se evita que la clase se llame igual que su namespace (error CS0118 al usarla).
namespace SocialManagement.Solicitudes.Application;

/// <summary>
/// Registro de la capa de APLICACIÓN. Cada capa de Clean Architecture tiene su propio <c>DependencyInjection.cs</c>
/// (Application, Infrastructure y Api); Program.cs solo los encadena:
/// <code>builder.Services.AddApplication().AddInfrastructure().AddPresentation();</code>
/// Domain no tiene: no depende de ningún paquete ni registra servicios.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todo lo de esta capa buscándolo en el ensamblado, sin listas manuales:
    /// validadores (FluentValidation), perfiles de AutoMapper (carpeta Mappings) y casos de uso (clases
    /// <c>*Handler</c> de la carpeta Features). Aún no hay ninguno en Solicitudes: el primero se registrará solo.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // La clave de licencia de AutoMapper la configura la capa Api (lee la configuración); aquí solo los perfiles.
        services.AddAutoMapper(_ => { }, assembly);

        foreach (var handler in UseCaseHandlers(assembly))
        {
            services.AddScoped(handler);
        }

        return services;
    }

    /// <summary>Clases concretas de casos de uso: el nombre termina en "Handler" (convención de la carpeta Features).</summary>
    internal static IEnumerable<Type> UseCaseHandlers(Assembly assembly) =>
        assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false, IsNested: false }
            && type.Name.EndsWith("Handler", StringComparison.Ordinal));
}
