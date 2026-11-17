using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Api.Contracts.Common;
using SocialManagement.Beneficiarios.Api.Extensions;

namespace SocialManagement.Beneficiarios.Api.Controllers;

/// <summary>
/// Base de todos los controllers de recursos del microservicio (p. ej. un futuro BeneficiariosController).
/// Centraliza en un solo lugar:
/// <list type="bullet">
/// <item><b>Ruta versionada</b>: <c>api/v{version:apiVersion}/[controller]</c> en minúsculas
/// (BeneficiariosController → /api/v1/beneficiarios). Sin [ApiVersion], un controller es versión 1.0.</item>
/// <item><b>Formato de respuesta</b>: <see cref="ApiResponse{T}"/> en éxitos y errores. Los errores de validación (400),
/// de dominio (422) e inesperados (500) también usan este formato (ver GlobalExceptionHandler y AddApiPresentation).</item>
/// <item><b>Documentación OpenAPI</b> de las respuestas de error comunes.</item>
/// <item><b>Dependencias comunes</b> sin inyección por constructor: <see cref="Logger"/>, <see cref="TraceId"/>, <see cref="Mapper"/> y los casos de
/// uso de Application con <see cref="UseCase{TUseCase}"/>.</item>
/// </list>
/// </summary>
/// <remarks>
/// Flujo de Clean Architecture: el controller obtiene un caso de uso y lo ejecuta; el caso de uso usa los
/// repositorios y confirma los cambios con IUnitOfWork. El controller nunca accede a la persistencia.
/// <code>
/// public sealed class BeneficiariosController : BaseApiController
/// {
///     [HttpGet("{id:int}")]
///     [ProducesResponseType&lt;ApiResponse&lt;BeneficiarioDto&gt;&gt;(StatusCodes.Status200OK)]
///     public async Task&lt;ActionResult&lt;ApiResponse&lt;BeneficiarioDto&gt;&gt;&gt; ObtenerPorId(int id, CancellationToken ct)
///     {
///         var beneficiario = await UseCase&lt;ConsultarBeneficiarioHandler&gt;().HandleAsync(new(id), ct);
///         return ApiOkOrNotFound(beneficiario, $"No existe el beneficiario {id}.");
///     }
/// }
/// </code>
/// Las dependencias se resuelven de <c>HttpContext.RequestServices</c> solo cuando se usan. En pruebas unitarias,
/// asigna un <c>ControllerContext</c> con un <c>HttpContext</c> cuyo <c>RequestServices</c> contenga los dobles de prueba.
/// </remarks>
[ApiController]
[Route(RouteTemplate)]
[Produces("application/json")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>Formato estándar de ruta de todos los controllers: versión en la URL.</summary>
    public const string RouteTemplate = "api/v{version:apiVersion}/[controller]";

    private ILogger? _logger;
    private IMapper? _mapper;

    /// <summary>Logger con la categoría del controller concreto.</summary>
    protected ILogger Logger => _logger ??= Services.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

    /// <summary>Identificador de traza de la petición actual (se incluye en cada <see cref="ApiResponse{T}"/>).</summary>
    protected string TraceId => HttpContext.GetTraceId();

    /// <summary>Contenedor de DI de la petición actual.</summary>
    protected IServiceProvider Services => HttpContext.RequestServices;

    /// <summary>Mapeador de AutoMapper (perfiles de Mappings en Api y Application): request → comando.</summary>
    protected IMapper Mapper => _mapper ??= Services.GetRequiredService<IMapper>();

    /// <summary>Obtiene un caso de uso de Application registrado en DI (p. ej. RegistrarBeneficiarioHandler).</summary>
    protected TUseCase UseCase<TUseCase>()
        where TUseCase : notnull =>
        Services.GetRequiredService<TUseCase>();

    /// <summary>200 OK con los datos en el formato estándar.</summary>
    protected OkObjectResult ApiOk<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, message, TraceId));

    /// <summary>201 Created con la cabecera Location apuntando a la acción indicada.</summary>
    protected CreatedAtActionResult ApiCreated<T>(string actionName, object routeValues, T data, string? message = null) =>
        CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data, message, TraceId));

    /// <summary>404 Not Found en el formato estándar.</summary>
    protected NotFoundObjectResult ApiNotFound(string message) =>
        NotFound(ApiResponse<object>.Fail(message, traceId: TraceId));

    /// <summary>200 con los datos, o 404 con el mensaje indicado si los datos son null.</summary>
    protected ActionResult ApiOkOrNotFound<T>(T? data, string notFoundMessage)
        where T : class =>
        data is null ? ApiNotFound(notFoundMessage) : ApiOk(data);
}
