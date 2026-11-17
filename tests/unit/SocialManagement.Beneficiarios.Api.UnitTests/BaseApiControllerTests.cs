using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SocialManagement.Beneficiarios.Api.Contracts.Common;
using SocialManagement.Beneficiarios.Api.Controllers;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>
/// Pruebas unitarias de <see cref="BaseApiController"/>: formato de respuesta, rutas y dependencias comunes.
/// El controller se crea con <c>new</c>; las dependencias llegan por el RequestServices del HttpContext.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BaseApiControllerTests
{
    private readonly TestResourceController _controller;

    public BaseApiControllerTests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddScoped<FakeUseCase>()
            .BuildServiceProvider();

        _controller = new TestResourceController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services, TraceIdentifier = "traza-de-prueba" },
            },
        };
    }

    [Fact]
    public void ApiOk_envuelve_los_datos_en_ApiResponse()
    {
        var result = _controller.ApiOk("dato", "Consulta correcta");

        var response = Assert.IsType<ApiResponse<string>>(result.Value);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.True(response.Success);
        Assert.Equal("dato", response.Data);
        Assert.Equal("Consulta correcta", response.Message);
        Assert.Empty(response.Errors);
        Assert.Equal(_controller.TraceId, response.TraceId);
    }

    [Fact]
    public void ApiOkOrNotFound_devuelve_404_con_el_mensaje_si_no_hay_datos()
    {
        var result = _controller.ApiOkOrNotFound<string>(null, "No existe el recurso 7.");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("No existe el recurso 7.", response.Message);
    }

    [Fact]
    public void ApiOkOrNotFound_devuelve_200_si_hay_datos()
    {
        var result = _controller.ApiOkOrNotFound("dato", "No existe.");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("dato", Assert.IsType<ApiResponse<string>>(ok.Value).Data);
    }

    [Fact]
    public void ApiCreated_devuelve_201_con_la_accion_y_los_valores_de_ruta()
    {
        var result = _controller.ApiCreated("ObtenerPorId", new { id = 7 }, "creado");

        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal("ObtenerPorId", result.ActionName);
        Assert.Equal(7, result.RouteValues!["id"]);
        Assert.True(Assert.IsType<ApiResponse<string>>(result.Value).Success);
    }

    [Fact]
    public void UseCase_obtiene_el_caso_de_uso_del_contenedor_sin_inyeccion_por_constructor()
    {
        var useCase = _controller.UseCase<FakeUseCase>();

        Assert.Equal("ejecutado", useCase.Execute());
    }

    [Fact]
    public void Logger_y_TraceId_estan_disponibles()
    {
        Assert.NotNull(_controller.Logger);
        Assert.False(string.IsNullOrWhiteSpace(_controller.TraceId));
    }

    [Fact]
    public void Define_la_ruta_y_el_comportamiento_comunes_para_todos_los_controllers()
    {
        var type = typeof(BaseApiController);

        Assert.True(type.IsAbstract);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/v{version:apiVersion}/[controller]", type.GetCustomAttribute<RouteAttribute>()!.Template);

        var errorCodes = type.GetCustomAttributes<ProducesResponseTypeAttribute>().Select(a => a.StatusCode).Order();
        Assert.Equal([400, 422, 500], errorCodes);
    }
}
