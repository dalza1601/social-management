using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocialManagement.Beneficiarios.Api.Configuration;
using SocialManagement.Beneficiarios.Api.Contracts.Diagnostics;

namespace SocialManagement.Beneficiarios.Api.Endpoints;

/// <summary>
/// Minimal API de demostración (sesión 01): GET /api/demo/status.
/// </summary>
/// <remarks>
/// Un endpoint simple no necesita un controller: basta una función que <c>app.MapGet</c> registra
/// en Program.cs. La función vive en esta clase para poder probarla de forma unitaria.
/// Las dependencias llegan como parámetros resueltos por el contenedor de DI:
/// - <see cref="IOptions{TOptions}"/>: nombre del servicio (appsettings.json, sección ServiceInfo).
/// - <see cref="TimeProvider"/>: la hora actual; en las pruebas se reemplaza por un reloj falso.
/// - <see cref="IHostApplicationLifetime"/>: indica si la aplicación se está deteniendo.
/// </remarks>
public static class GetDemoStatusEndpoint
{
    public const string Route = "/api/demo/status";
    public const string RunningStatus = "Running";
    public const string StoppingStatus = "Stopping";

    // Sin comentario XML a propósito: el resumen y la descripción de OpenAPI se declaran con
    // WithSummary/WithDescription en Program.cs (un <summary> aquí los reemplazaría).
    public static Results<Ok<DemoStatusResponse>, JsonHttpResult<DemoStatusResponse>> Handle(
        [FromServices] IOptions<ServiceInfoOptions> serviceInfo,
        [FromServices] TimeProvider timeProvider,
        [FromServices] IHostApplicationLifetime lifetime)
    {
        var isStopping = lifetime.ApplicationStopping.IsCancellationRequested;

        var response = new DemoStatusResponse(
            ServiceName: serviceInfo.Value.Name,
            Version: ServiceVersion.Current,
            TimestampUtc: timeProvider.GetUtcNow(),
            Status: isStopping ? StoppingStatus : RunningStatus);

        return isStopping
            ? TypedResults.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable)
            : TypedResults.Ok(response);
    }
}
