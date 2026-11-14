using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocialManagement.Beneficiarios.Api.Configuration;

namespace SocialManagement.Beneficiarios.Api.Controllers;

/// <summary>
/// Endpoint DEMOSTRATIVO de la sesión 01: muestra la anatomía de un controller de ASP.NET Core
/// (ruta, inyección de dependencias, códigos HTTP y metadatos OpenAPI).
/// No contiene entidades ni reglas de negocio.
/// </summary>
/// <remarks>
/// Todas las dependencias llegan por el constructor (inyección de dependencias):
/// - <see cref="IOptions{TOptions}"/>: nombre del servicio, desde appsettings.json (sección ServiceInfo).
/// - <see cref="TimeProvider"/>: la hora actual. Es una abstracción: en producción es el reloj del
///   sistema y en las pruebas un reloj falso (FakeTimeProvider) con una hora fija.
/// - <see cref="IHostApplicationLifetime"/>: indica si la aplicación se está deteniendo.
/// </remarks>
[ApiController]
[Route("api/demo")]
[Produces("application/json")]
[Tags("Demo")]
public sealed class DemoController(
    IOptions<ServiceInfoOptions> serviceInfo,
    TimeProvider timeProvider,
    IHostApplicationLifetime lifetime) : ControllerBase
{
    public const string RunningStatus = "Running";
    public const string StoppingStatus = "Stopping";

    // Con GenerateDocumentationFile, este <summary> se publica como "summary" en OpenAPI
    // (prevalece sobre [EndpointSummary]): ambos textos deben coincidir.
    /// <summary>Estado de ejecución del servicio (demostración)</summary>
    /// <response code="200">El servicio está en ejecución.</response>
    /// <response code="503">El servicio se está deteniendo y no debe recibir más tráfico.</response>
    [HttpGet("status")]
    [EndpointName("GetDemoStatus")]
    [EndpointSummary("Estado de ejecución del servicio (demostración)")]
    [EndpointDescription(
        "Endpoint didáctico de la sesión 01. Devuelve el nombre del servicio, su versión, " +
        "la fecha y hora UTC del servidor y el estado de ejecución (Running o Stopping).")]
    [ProducesResponseType<DemoStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DemoStatusResponse>(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<DemoStatusResponse> GetStatus()
    {
        var isStopping = lifetime.ApplicationStopping.IsCancellationRequested;

        var response = new DemoStatusResponse(
            ServiceName: serviceInfo.Value.Name,
            Version: ServiceVersion.Current,
            TimestampUtc: timeProvider.GetUtcNow(),
            Status: isStopping ? StoppingStatus : RunningStatus);

        return isStopping
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, response)
            : Ok(response);
    }
}

/// <summary>Respuesta de GET /api/demo/status.</summary>
/// <param name="ServiceName">Nombre del servicio (appsettings: ServiceInfo:Name).</param>
/// <param name="Version">Versión del ensamblado de la API.</param>
/// <param name="TimestampUtc">Fecha y hora UTC del servidor al responder.</param>
/// <param name="Status">Estado de ejecución: Running o Stopping.</param>
public sealed record DemoStatusResponse(
    string ServiceName,
    string Version,
    DateTimeOffset TimestampUtc,
    string Status);
