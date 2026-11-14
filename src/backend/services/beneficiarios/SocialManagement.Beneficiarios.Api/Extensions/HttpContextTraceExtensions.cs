using System.Diagnostics;

namespace SocialManagement.Beneficiarios.Api.Extensions;

/// <summary>Identificador de traza de la petición, igual en las respuestas de error y en los logs.</summary>
public static class HttpContextTraceExtensions
{
    public static string GetTraceId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
