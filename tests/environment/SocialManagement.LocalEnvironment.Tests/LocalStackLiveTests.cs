using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;

namespace SocialManagement.LocalEnvironment.Tests;

/// <summary>Recursos AWS aprovisionados en LocalStack. Se omiten salvo que SM_LOCALSTACK_TESTS=true.</summary>
[Trait("Category", "Environment")]
public sealed class LocalStackLiveTests
{
    private readonly string _serviceUrl = $"http://localhost:{LocalEnvironment.GetPort("LOCALSTACK_PORT", 4566)}";
    private readonly BasicAWSCredentials _credentials =
        new(LocalEnvironment.Get("AWS_ACCESS_KEY_ID", "test"), LocalEnvironment.Get("AWS_SECRET_ACCESS_KEY", "test"));
    private readonly string _region = LocalEnvironment.Get("AWS_REGION", "us-east-1");

    public LocalStackLiveTests() => Assert.SkipUnless(
        LocalEnvironment.LocalStackTestsEnabled,
        "Requiere LocalStack en ejecución (perfil 'localstack'). Actívalo con SM_LOCALSTACK_TESTS=true.");

    [Fact]
    public async Task Existe_el_bucket_de_documentos()
    {
        using var s3 = new AmazonS3Client(_credentials, new AmazonS3Config { ServiceURL = _serviceUrl, ForcePathStyle = true, AuthenticationRegion = _region });

        var buckets = await s3.ListBucketsAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            LocalEnvironment.Get("AWS_S3_DOCUMENTS_BUCKET", "social-management-documents-dev"),
            (buckets.Buckets ?? []).Select(b => b.BucketName));
    }

    [Fact]
    public async Task Existe_la_cola_de_notificaciones()
    {
        using var sqs = new AmazonSQSClient(_credentials, new AmazonSQSConfig { ServiceURL = _serviceUrl, AuthenticationRegion = _region });

        var queue = await sqs.GetQueueUrlAsync(
            LocalEnvironment.Get("AWS_SQS_NOTIFICATIONS_QUEUE", "social-management-notifications-dev"),
            TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(queue.QueueUrl));
    }
}
