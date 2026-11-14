using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>Doble de prueba de <see cref="IHostEnvironment"/> con un nombre de entorno fijo.</summary>
internal sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "SocialManagement.Beneficiarios.Api";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
