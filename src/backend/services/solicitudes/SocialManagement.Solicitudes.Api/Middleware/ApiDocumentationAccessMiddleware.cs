using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SocialManagement.Solicitudes.Api.Configuration;

namespace SocialManagement.Solicitudes.Api.Middleware;

/// <summary>
/// Protege Swagger UI y el documento OpenAPI con autenticación HTTP Basic cuando
/// <c>ApiDocumentation:Access</c> tiene usuario y contraseña. El resto de endpoints no se ve afectado.
/// </summary>
/// <remarks>
/// HTTP Basic solo es seguro sobre HTTPS: en ambientes desplegados el Gateway o el ingress
/// deben terminar TLS. Cuando el proyecto incorpore JWT (módulo de seguridad), esta protección
/// podrá reemplazarse por una política de autorización.
/// </remarks>
internal sealed class ApiDocumentationAccessMiddleware(
    RequestDelegate next,
    IOptions<ApiDocumentationOptions> options,
    ILogger<ApiDocumentationAccessMiddleware> logger)
{
    private const string Realm = "SocialManagement API documentation";

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = options.Value;

        if (!settings.Access.IsConfigured || !IsDocumentationPath(context.Request.Path, settings))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (TryReadBasicCredentials(header, out var userName, out var password)
            && FixedTimeEquals(userName, settings.Access.UserName!)
            && FixedTimeEquals(password, settings.Access.Password!))
        {
            await next(context);
            return;
        }

        if (header.Length > 0)
        {
            logger.LogWarning(
                "Credenciales inválidas para la documentación de la API ({Path}) desde {RemoteIp}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers[HeaderNames.WWWAuthenticate] = $"Basic realm=\"{Realm}\", charset=\"UTF-8\"";
    }

    private static bool IsDocumentationPath(PathString path, ApiDocumentationOptions settings) =>
        path.StartsWithSegments($"/{settings.RoutePrefix}", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments($"/{ApiDocumentationOptions.OpenApiRoutePrefix}", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadBasicCredentials(string header, out string userName, out string password)
    {
        userName = password = string.Empty;
        const string prefix = "Basic ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header[prefix.Length..].Trim()));
            var separator = decoded.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                return false;
            }

            userName = decoded[..separator];
            password = decoded[(separator + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Compara resúmenes SHA-256 en tiempo constante: no revela por tiempos ni la longitud ni el contenido.
    private static bool FixedTimeEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
