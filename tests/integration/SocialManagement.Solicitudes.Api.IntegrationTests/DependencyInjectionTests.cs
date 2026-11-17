using Amazon.S3;
using AutoMapper;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SocialManagement.Solicitudes.Application.Interfaces;
using SocialManagement.Solicitudes.Infrastructure.Configurations;
using SocialManagement.Solicitudes.Infrastructure.Messaging;
using SocialManagement.Solicitudes.Infrastructure.Persistence;

namespace SocialManagement.Solicitudes.Api.IntegrationTests;

/// <summary>
/// Verifica que el contenedor de DI resuelve los servicios de infraestructura y que las
/// opciones quedan enlazadas con la configuración. (Program.cs además usa ValidateOnBuild.)
/// </summary>
[Trait("Category", "Integration")]
public sealed class DependencyInjectionTests(SolicitudesApiFactory factory) : IClassFixture<SolicitudesApiFactory>
{
    [Fact]
    public void Resuelve_el_mapeador_de_AutoMapper_aunque_aun_no_haya_perfiles()
    {
        // AddApplication y AddPresentation ya registran AutoMapper: el primer perfil de Solicitudes se cargará solo.
        var mapper = factory.Services.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Resuelve_el_DbContext_propio_del_servicio()
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>();

        Assert.Equal("solicitudes", dbContext.Model.GetDefaultSchema());
    }

    [Fact]
    public void El_Unit_of_Work_es_el_mismo_DbContext_de_la_peticion()
    {
        using var scope = factory.Services.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>();

        // Misma instancia: lo que registran los repositorios se confirma con IUnitOfWork.SaveChangesAsync.
        Assert.Same(dbContext, unitOfWork);
    }

    [Fact]
    public void Resuelve_la_conexion_de_RabbitMQ_como_singleton()
    {
        var first = factory.Services.GetRequiredService<IRabbitMqConnectionProvider>();
        var second = factory.Services.GetRequiredService<IRabbitMqConnectionProvider>();

        Assert.Same(first, second);
    }

    [Fact]
    public void Configura_los_clientes_AWS_contra_LocalStack()
    {
        var s3 = factory.Services.GetRequiredService<IAmazonS3>();
        var sqs = factory.Services.GetRequiredService<IAmazonSQS>();

        Assert.StartsWith(SolicitudesApiFactory.LocalStackUrl, s3.Config.ServiceURL, StringComparison.Ordinal);
        Assert.True(((AmazonS3Config)s3.Config).ForcePathStyle);
        Assert.StartsWith(SolicitudesApiFactory.LocalStackUrl, sqs.Config.ServiceURL, StringComparison.Ordinal);
        Assert.Equal("us-east-1", s3.Config.AuthenticationRegion);
    }

    [Fact]
    public void Enlaza_las_opciones_de_infraestructura()
    {
        var rabbit = factory.Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var aws = factory.Services.GetRequiredService<IOptions<AwsOptions>>().Value;
        var ollama = factory.Services.GetRequiredService<IOptions<OllamaOptions>>().Value;

        Assert.Equal("rabbitmq.test", rabbit.Host);
        Assert.Equal("social-management", rabbit.VirtualHost);
        Assert.Equal("social-management-documents-dev", aws.DocumentsBucketName);
        Assert.Equal("social-management-notifications-dev", aws.NotificationsQueueName);
        Assert.False(ollama.Enabled);
    }

    [Fact]
    public void Registra_health_checks_de_disponibilidad_y_de_dependencias()
    {
        var options = factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registrations = options.Registrations.ToDictionary(r => r.Name);

        Assert.Contains("live", registrations["self"].Tags);
        Assert.Contains("ready", registrations["sqlserver"].Tags);
        Assert.Contains("ready", registrations["rabbitmq"].Tags);
    }
}
