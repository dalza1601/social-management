using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Api.Configuration;
using SocialManagement.Beneficiarios.Api.Endpoints;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>Pruebas unitarias del Minimal API GET /version.</summary>
[Trait("Category", "Unit")]
public sealed class GetVersionEndpointTests
{
    [Fact]
    public void Devuelve_nombre_version_entorno_framework_y_fecha_del_reloj()
    {
        var now = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

        var result = GetVersionEndpoint.Handle(
            Options.Create(new ServiceInfoOptions { Name = "beneficiarios-api" }),
            new FakeHostEnvironment("Staging"),
            new FakeTimeProvider(now));

        var version = result.Value!;
        Assert.Equal("beneficiarios-api", version.Service);
        Assert.Equal(ServiceVersion.Current, version.Version);
        Assert.Equal("Staging", version.Environment);
        Assert.Equal(RuntimeInformation.FrameworkDescription, version.Framework);
        Assert.Equal(now, version.TimestampUtc);
    }
}
