using Microsoft.Extensions.DependencyInjection;

namespace SocialManagement.Solicitudes.Application;

/// <summary>
/// Punto único de registro de los servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de aplicación (casos de uso, validadores y behaviors).
    /// </summary>
    /// <remarks>
    /// Fase 0: todavía no existen casos de uso, por lo que no se registra nada.
    /// Cuando se implementen Features, Validators y Behaviors, se registrarán aquí
    /// para que la API solo tenga que invocar <c>AddApplication()</c>.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
