using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocialManagement.Solicitudes.Api.Configuration;
using SocialManagement.Solicitudes.Api.Contracts;

namespace SocialManagement.Solicitudes.Api.Endpoints;

/// <summary>
/// Minimal API técnico: GET /version. Informa qué versión del servicio está en ejecución.
/// La ruta se registra con <c>app.MapGet</c> en Program.cs.
/// </summary>
public static class GetVersionEndpoint
{
    public const string Route = "/version";

    // Sin comentario XML a propósito: el resumen de OpenAPI se declara con WithSummary en Program.cs.
    public static Ok<VersionResponse> Handle(
        [FromServices] IOptions<ServiceInfoOptions> serviceInfo,
        [FromServices] IHostEnvironment environment,
        [FromServices] TimeProvider timeProvider) =>
        TypedResults.Ok(new VersionResponse(
            serviceInfo.Value.Name,
            ServiceVersion.Current,
            environment.EnvironmentName,
            RuntimeInformation.FrameworkDescription,
            timeProvider.GetUtcNow()));
}
