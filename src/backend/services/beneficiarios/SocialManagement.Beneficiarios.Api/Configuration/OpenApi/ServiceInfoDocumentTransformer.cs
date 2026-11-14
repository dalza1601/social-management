using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace SocialManagement.Beneficiarios.Api.Configuration.OpenApi;

/// <summary>Completa el nombre, la descripción y la versión del documento OpenAPI desde appsettings (ServiceInfo).</summary>
internal sealed class ServiceInfoDocumentTransformer(IOptions<ServiceInfoOptions> serviceInfo) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var info = serviceInfo.Value;
        document.Info.Title = $"SocialManagement · {info.Name}";
        document.Info.Description = info.Description;
        document.Info.Version = ServiceVersion.Current;

        // "http://host:8081/" -> "http://host:8081": los validadores OpenAPI marcan la barra final como error.
        // Las URL de "servers" reflejan la URL pública (incluido el prefijo del Gateway, ver ForwardedHeaders).
        foreach (var server in document.Servers ?? [])
        {
            server.Url = server.Url?.TrimEnd('/');
        }

        return Task.CompletedTask;
    }
}
