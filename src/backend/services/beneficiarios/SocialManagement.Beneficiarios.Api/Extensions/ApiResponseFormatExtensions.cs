using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using SocialManagement.Beneficiarios.Api.Controllers;

namespace SocialManagement.Beneficiarios.Api.Extensions;

/// <summary>Decide en qué formato se escribe una respuesta de error generada fuera del controller.</summary>
public static class ApiResponseFormatExtensions
{
    /// <summary>
    /// ¿La petición corresponde a una acción de un controller que hereda de <see cref="BaseApiController"/>?
    /// Entonces los errores (excepciones, 401, 403) usan ApiResponse; si no, ProblemDetails (RFC 9457).
    /// </summary>
    public static bool UsesApiResponseFormat(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // El middleware de excepciones limpia el endpoint actual; el original se conserva en IExceptionHandlerFeature.
        var endpoint = httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint ?? httpContext.GetEndpoint();
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        return action is not null && action.ControllerTypeInfo.IsSubclassOf(typeof(BaseApiController));
    }
}
