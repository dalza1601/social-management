using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>Controller SOLO de pruebas: simula un futuro endpoint protegido con [Authorize].</summary>
[ApiController]
[Route("pruebas/protegido")]
[Authorize]
public sealed class ProtectedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
