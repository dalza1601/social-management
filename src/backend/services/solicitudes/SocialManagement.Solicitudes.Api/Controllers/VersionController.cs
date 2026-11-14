using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocialManagement.Solicitudes.Api.Configuration;

namespace SocialManagement.Solicitudes.Api.Controllers;

/// <summary>Endpoint técnico que informa qué versión del servicio está en ejecución.</summary>
[ApiController]
[Route("version")]
[Produces("application/json")]
public sealed class VersionController(
    IOptions<ServiceInfoOptions> serviceInfo,
    IHostEnvironment environment) : ControllerBase
{
    /// <summary>Devuelve el nombre, la versión y el entorno del servicio.</summary>
    [HttpGet]
    [ProducesResponseType<VersionResponse>(StatusCodes.Status200OK)]
    public ActionResult<VersionResponse> Get()
    {
        return Ok(new VersionResponse(
            serviceInfo.Value.Name,
            ServiceVersion.Current,
            environment.EnvironmentName,
            RuntimeInformation.FrameworkDescription,
            DateTimeOffset.UtcNow));
    }
}

/// <summary>Respuesta del endpoint /version.</summary>
public sealed record VersionResponse(
    string Service,
    string Version,
    string Environment,
    string Framework,
    DateTimeOffset TimestampUtc);
