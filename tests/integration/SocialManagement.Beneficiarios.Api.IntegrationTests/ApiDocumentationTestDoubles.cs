using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// TestServer no tiene dirección IP remota. Este filtro simula desde qué IP llega la petición
/// para probar qué proxies son de confianza en los encabezados X-Forwarded-*.
/// </summary>
internal sealed class RemoteIpStartupFilter(IPAddress remoteIp) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = remoteIp;
            return nextMiddleware(context);
        });
        next(app);
    };
}

/// <summary>Esquema de autenticación "Bearer" mínimo: solo para comprobar el documento OpenAPI.</summary>
internal sealed class TestBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}

/// <summary>Controller SOLO de pruebas: simula un futuro endpoint protegido con [Authorize].</summary>
[ApiController]
[Route("pruebas/protegido")]
[Authorize]
public sealed class ProtectedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
