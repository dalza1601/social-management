using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SocialManagement.Beneficiarios.Application;

/// <summary>
/// Punto único de registro de los servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de aplicación (casos de uso, validadores y behaviors).
    /// </summary>
    /// <remarks>
    /// - Validadores (FluentValidation): todos los AbstractValidator&lt;T&gt; de este ensamblado, como IValidator&lt;T&gt;.
    /// - Casos de uso: RegistrarBeneficiarioHandler y ConsultarBeneficiarioHandler existen, pero TODAVÍA NO se
    ///   registran. Necesitan un IBeneficiarioRepository y aún no hay ninguna implementación; con
    ///   ValidateOnBuild = true (Program.cs) la API no arrancaría. Se registrarán aquí junto con el repositorio
    ///   de EF Core en la sesión de persistencia:
    ///     services.AddScoped&lt;RegistrarBeneficiarioHandler&gt;();
    ///     services.AddScoped&lt;ConsultarBeneficiarioHandler&gt;();
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
