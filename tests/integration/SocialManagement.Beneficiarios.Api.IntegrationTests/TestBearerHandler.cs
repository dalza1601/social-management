using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>Esquema de autenticación "Bearer" mínimo: solo para comprobar el documento OpenAPI.</summary>
internal sealed class TestBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}
