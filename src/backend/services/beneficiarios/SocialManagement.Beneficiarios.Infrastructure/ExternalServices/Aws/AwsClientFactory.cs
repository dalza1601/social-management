using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using SocialManagement.Beneficiarios.Infrastructure.Configurations;

namespace SocialManagement.Beneficiarios.Infrastructure.ExternalServices.Aws;

/// <summary>
/// Construye los clientes del AWS SDK for .NET a partir de <see cref="AwsOptions"/>.
/// Centraliza la decisión "LocalStack o AWS real" en un solo lugar.
/// </summary>
/// <remarks>Fase 0: los clientes se registran pero ningún caso de uso los utiliza todavía.</remarks>
public static class AwsClientFactory
{
    public static IAmazonS3 CreateS3Client(AwsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var config = new AmazonS3Config();

        if (options.UseLocalStack)
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.Region;
            // LocalStack expone los buckets como rutas (http://host:4566/bucket), no como subdominios.
            config.ForcePathStyle = true;
            return new AmazonS3Client(CreateLocalStackCredentials(options), config);
        }

        config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        return new AmazonS3Client(config);
    }

    public static IAmazonSQS CreateSqsClient(AwsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var config = new AmazonSQSConfig();

        if (options.UseLocalStack)
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.Region;
            return new AmazonSQSClient(CreateLocalStackCredentials(options), config);
        }

        config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        return new AmazonSQSClient(config);
    }

    private static BasicAWSCredentials CreateLocalStackCredentials(AwsOptions options) =>
        new(options.AccessKey, options.SecretKey);
}
