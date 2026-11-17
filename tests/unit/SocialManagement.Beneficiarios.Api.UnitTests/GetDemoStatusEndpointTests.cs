using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Api.Configuration;
using SocialManagement.Beneficiarios.Api.Contracts.Diagnostics;
using SocialManagement.Beneficiarios.Api.Endpoints;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>
/// Pruebas unitarias del Minimal API GET /api/demo/status.
/// La función del endpoint se invoca directamente (sin servidor HTTP), con un reloj falso.
/// Patrón Arrange / Act / Assert.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GetDemoStatusEndpointTests : IDisposable
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(FixedNow);
    private readonly FakeHostApplicationLifetime _lifetime = new();
    private readonly IOptions<ServiceInfoOptions> _serviceInfo =
        Options.Create(new ServiceInfoOptions { Name = "beneficiarios-api-test" });

    [Fact]
    public void Devuelve_200_con_nombre_version_fecha_UTC_y_estado_Running()
    {
        // Act
        var result = GetDemoStatusEndpoint.Handle(_serviceInfo, _clock, _lifetime);

        // Assert
        var ok = Assert.IsType<Ok<DemoStatusResponse>>(result.Result);
        var status = Assert.IsType<DemoStatusResponse>(ok.Value);
        Assert.Equal("beneficiarios-api-test", status.ServiceName);
        Assert.Equal(ServiceVersion.Current, status.Version);
        Assert.Equal(FixedNow, status.TimestampUtc);
        Assert.Equal(TimeSpan.Zero, status.TimestampUtc.Offset);
        Assert.Equal(GetDemoStatusEndpoint.RunningStatus, status.Status);
    }

    [Fact]
    public void La_fecha_proviene_del_TimeProvider_inyectado()
    {
        // Arrange: el reloj falso avanza 90 minutos.
        _clock.Advance(TimeSpan.FromMinutes(90));

        // Act
        var result = GetDemoStatusEndpoint.Handle(_serviceInfo, _clock, _lifetime);

        // Assert
        var ok = Assert.IsType<Ok<DemoStatusResponse>>(result.Result);
        Assert.Equal(FixedNow.AddMinutes(90), ok.Value!.TimestampUtc);
    }

    [Fact]
    public void Devuelve_503_y_estado_Stopping_cuando_la_aplicacion_se_detiene()
    {
        // Arrange
        _lifetime.StopApplication();

        // Act
        var result = GetDemoStatusEndpoint.Handle(_serviceInfo, _clock, _lifetime);

        // Assert
        var json = Assert.IsType<JsonHttpResult<DemoStatusResponse>>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, json.StatusCode);
        Assert.Equal(GetDemoStatusEndpoint.StoppingStatus, json.Value!.Status);
    }

    [Fact]
    public void La_version_del_servicio_coincide_con_Directory_Build_props()
    {
        Assert.StartsWith("0.1.0", ServiceVersion.Current, StringComparison.Ordinal);
    }

    public void Dispose() => _lifetime.Dispose();
}
