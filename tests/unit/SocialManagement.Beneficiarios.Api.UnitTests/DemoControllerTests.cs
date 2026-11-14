using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Api.Configuration;
using SocialManagement.Beneficiarios.Api.Controllers;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>
/// Pruebas unitarias del endpoint demostrativo GET /api/demo/status.
/// Patrón Arrange / Act / Assert. El reloj es falso: la hora es fija y predecible.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DemoControllerTests : IDisposable
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(FixedNow);
    private readonly FakeHostApplicationLifetime _lifetime = new();
    private readonly DemoController _controller;

    public DemoControllerTests()
    {
        var serviceInfo = Options.Create(new ServiceInfoOptions { Name = "beneficiarios-api-test" });
        _controller = new DemoController(serviceInfo, _clock, _lifetime);
    }

    [Fact]
    public void Devuelve_200_con_nombre_version_fecha_UTC_y_estado_Running()
    {
        // Act
        var result = _controller.GetStatus();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<DemoStatusResponse>(ok.Value);
        Assert.Equal("beneficiarios-api-test", status.ServiceName);
        Assert.Equal(ServiceVersion.Current, status.Version);
        Assert.Equal(FixedNow, status.TimestampUtc);
        Assert.Equal(TimeSpan.Zero, status.TimestampUtc.Offset);
        Assert.Equal(DemoController.RunningStatus, status.Status);
    }

    [Fact]
    public void La_fecha_proviene_del_TimeProvider_inyectado()
    {
        // Arrange: el reloj falso avanza 90 minutos.
        _clock.Advance(TimeSpan.FromMinutes(90));

        // Act
        var status = (DemoStatusResponse)((OkObjectResult)_controller.GetStatus().Result!).Value!;

        // Assert
        Assert.Equal(FixedNow.AddMinutes(90), status.TimestampUtc);
    }

    [Fact]
    public void Devuelve_503_y_estado_Stopping_cuando_la_aplicacion_se_detiene()
    {
        // Arrange
        _lifetime.StopApplication();

        // Act
        var result = _controller.GetStatus();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var status = Assert.IsType<DemoStatusResponse>(objectResult.Value);
        Assert.Equal(DemoController.StoppingStatus, status.Status);
    }

    [Fact]
    public void La_version_del_servicio_coincide_con_Directory_Build_props()
    {
        Assert.StartsWith("0.1.0", ServiceVersion.Current, StringComparison.Ordinal);
    }

    public void Dispose() => _lifetime.Dispose();
}
