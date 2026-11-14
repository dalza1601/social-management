using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SocialManagement.Beneficiarios.Api.Controllers;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>Controller de prueba: expone los miembros protegidos de <see cref="BaseApiController"/>.</summary>
public sealed class TestResourceController : BaseApiController
{
    public new OkObjectResult ApiOk<T>(T data, string? message = null) => base.ApiOk(data, message);

    public new CreatedAtActionResult ApiCreated<T>(string actionName, object routeValues, T data, string? message = null) =>
        base.ApiCreated(actionName, routeValues, data, message);

    public new ActionResult ApiOkOrNotFound<T>(T? data, string notFoundMessage)
        where T : class =>
        base.ApiOkOrNotFound(data, notFoundMessage);

    public new TUseCase UseCase<TUseCase>()
        where TUseCase : notnull =>
        base.UseCase<TUseCase>();

    public new ILogger Logger => base.Logger;

    public new string TraceId => base.TraceId;
}
