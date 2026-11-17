using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.DesactivarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

namespace SocialManagement.Beneficiarios.Application;

/// <summary>
/// Punto único de registro de los servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de aplicación: validadores (FluentValidation) y casos de uso.
    /// </summary>
    /// <remarks>
    /// Los casos de uso dependen de IBeneficiarioRepository e IUnitOfWork, que registra Infrastructure
    /// (AddInfrastructure). Con ValidateOnBuild = true, si faltara alguno la API no arrancaría.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<RegistrarBeneficiarioHandler>();
        services.AddScoped<ConsultarBeneficiarioHandler>();
        services.AddScoped<ListarBeneficiariosHandler>();
        services.AddScoped<ActualizarBeneficiarioHandler>();
        services.AddScoped<DesactivarBeneficiarioHandler>();

        return services;
    }
}
