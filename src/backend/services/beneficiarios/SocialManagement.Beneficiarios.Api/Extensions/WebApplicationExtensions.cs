using Microsoft.Extensions.Options;
using SocialManagement.Beneficiarios.Api.Configuration;
using SocialManagement.Beneficiarios.Infrastructure.DependencyInjection;

namespace SocialManagement.Beneficiarios.Api.Extensions;

/// <summary>Configuración del pipeline HTTP y de los endpoints técnicos.</summary>
public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Primero: interpreta X-Forwarded-* de proxies de confianza (esquema, host y prefijo públicos).
        app.UseTrustedForwardedHeaders();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // /openapi/v1.json y /swagger, según ApiDocumentation:Enabled (por ambiente).
        app.UseApiDocumentation();

        // Sesión 04: primero se identifica al usuario (token JWT) y después se comprueba qué puede hacer (políticas).
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // /health       -> disponibilidad básica: el proceso responde (no consulta dependencias).
        // /health/ready -> dependencias: SQL Server y RabbitMQ.
        app.MapHealthChecks("/health", HealthCheckResponseWriter.CreateOptions(ApiServiceCollectionExtensions.LivenessTag));
        app.MapHealthChecks("/health/ready", HealthCheckResponseWriter.CreateOptions(InfrastructureServiceCollectionExtensions.ReadinessTag));

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var serviceInfo = app.Services.GetRequiredService<IOptions<ServiceInfoOptions>>().Value;
            app.Logger.LogInformation(
                "Servicio {ServiceName} iniciado en el entorno {Environment}",
                serviceInfo.Name,
                app.Environment.EnvironmentName);
        });

        return app;
    }
}
