using Amazon.S3;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocialManagement.Beneficiarios.Infrastructure.Configurations;
using SocialManagement.Beneficiarios.Infrastructure.ExternalServices.Aws;
using SocialManagement.Beneficiarios.Infrastructure.Messaging;
using SocialManagement.Beneficiarios.Infrastructure.Persistence;

namespace SocialManagement.Beneficiarios.Infrastructure.DependencyInjection;

/// <summary>
/// Registro de los servicios de infraestructura: persistencia, mensajería, AWS e IA local.
/// Todas las opciones se validan al iniciar la aplicación (ValidateOnStart): si falta
/// una configuración, el servicio no arranca y el log indica exactamente qué falta.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Etiqueta de los health checks que comprueban dependencias externas.</summary>
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddPersistence()
            .AddMessaging()
            .AddAwsServices()
            .AddLocalAi();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<SqlServerOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                options.ConnectionString = configuration.GetConnectionString(SqlServerOptions.ConnectionStringName) ?? string.Empty)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                ConfigurationMessages.Required(SqlServerOptions.ConfigurationKey))
            .ValidateOnStart();

        services.AddDbContext<BeneficiariosDbContext>((serviceProvider, options) =>
        {
            var sqlServer = serviceProvider.GetRequiredService<IOptions<SqlServerOptions>>().Value;
            options.UseSqlServer(sqlServer.ConnectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 3);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", BeneficiariosDbContext.DefaultSchema);
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<BeneficiariosDbContext>("sqlserver", tags: [ReadinessTag]);

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        const string section = RabbitMqOptions.SectionName;

        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(section)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), ConfigurationMessages.Required($"{section}:Host"))
            .Validate(o => o.Port is > 0 and <= 65535, ConfigurationMessages.Invalid($"{section}:Port", "debe estar entre 1 y 65535"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.VirtualHost), ConfigurationMessages.Required($"{section}:VirtualHost"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.UserName), ConfigurationMessages.Required($"{section}:UserName"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Password), ConfigurationMessages.Required($"{section}:Password"))
            .ValidateOnStart();

        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();

        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: [ReadinessTag], timeout: TimeSpan.FromSeconds(10));

        return services;
    }

    private static IServiceCollection AddAwsServices(this IServiceCollection services)
    {
        const string section = AwsOptions.SectionName;

        services.AddOptions<AwsOptions>()
            .BindConfiguration(section)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Region), ConfigurationMessages.Required($"{section}:Region"))
            .Validate(
                o => !o.UseLocalStack || ConfigurationMessages.IsAbsoluteHttpUri(o.ServiceUrl),
                ConfigurationMessages.Invalid($"{section}:ServiceUrl", "con UseLocalStack=true debe ser una URL http(s) absoluta, p. ej. http://localhost:4566"))
            .Validate(
                o => !o.UseLocalStack || (!string.IsNullOrWhiteSpace(o.AccessKey) && !string.IsNullOrWhiteSpace(o.SecretKey)),
                ConfigurationMessages.Invalid($"{section}:AccessKey/SecretKey", "con UseLocalStack=true se requieren credenciales ficticias (p. ej. 'test')"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.DocumentsBucketName), ConfigurationMessages.Required($"{section}:DocumentsBucketName"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.NotificationsQueueName), ConfigurationMessages.Required($"{section}:NotificationsQueueName"))
            .ValidateOnStart();

        // Los clientes del SDK no abren conexiones al crearse: solo cuando se invoca una operación.
        services.AddSingleton(sp => AwsClientFactory.CreateS3Client(sp.GetRequiredService<IOptions<AwsOptions>>().Value));
        services.AddSingleton(sp => AwsClientFactory.CreateSqsClient(sp.GetRequiredService<IOptions<AwsOptions>>().Value));

        return services;
    }

    private static IServiceCollection AddLocalAi(this IServiceCollection services)
    {
        const string section = OllamaOptions.SectionName;

        services.AddOptions<OllamaOptions>()
            .BindConfiguration(section)
            .Validate(
                o => !o.Enabled || ConfigurationMessages.IsAbsoluteHttpUri(o.Endpoint),
                ConfigurationMessages.Invalid($"{section}:Endpoint", "con Enabled=true debe ser una URL http(s) absoluta, p. ej. http://localhost:11434"))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.ModelId),
                ConfigurationMessages.Invalid($"{section}:ModelId", "con Enabled=true indica el modelo descargado manualmente (ver docs/infrastructure.md)"))
            .ValidateOnStart();

        return services;
    }
}
