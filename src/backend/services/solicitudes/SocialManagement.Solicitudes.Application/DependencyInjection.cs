using FluentValidation;
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
    /// - Validadores (FluentValidation): todos los AbstractValidator&lt;T&gt; de este ensamblado (carpeta Validators),
    ///   como IValidator&lt;T&gt;. Aún no hay comandos en Solicitudes; el primer validador se registrará solo.
    /// - Casos de uso: se registrarán aquí cuando existan (Features).
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
