using SocialManagement.Beneficiarios.Api.Contracts;
using SocialManagement.Beneficiarios.Api.Endpoints;
using SocialManagement.Beneficiarios.Api.Extensions;
using SocialManagement.Beneficiarios.Application;
using SocialManagement.Beneficiarios.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Valida el grafo de dependencias al construir la aplicación (falla rápido si falta un registro).
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Composición por capas: cada capa expone un único método de registro.
builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddApiPresentation();

var app = builder.Build();

app.UseApiPipeline();

// -----------------------------------------------------------------------------------------
// Minimal APIs: endpoints puntuales que no necesitan un controller. Aquí se registran la
// ruta y su documentación OpenAPI; cada función está en su propio archivo en Endpoints/.
// Los recursos con varias operaciones usan controllers que heredan de BaseApiController.
// -----------------------------------------------------------------------------------------
app.MapGet(GetVersionEndpoint.Route, GetVersionEndpoint.Handle)
    .WithName("GetVersion")
    .WithTags("Version")
    .WithSummary("Devuelve el nombre, la versión y el entorno del servicio.");

app.MapGet(GetDemoStatusEndpoint.Route, GetDemoStatusEndpoint.Handle)
    .WithName("GetDemoStatus")
    .WithTags("Demo")
    .WithSummary("Estado de ejecución del servicio (demostración)")
    .WithDescription(
        "Endpoint didáctico de la sesión 01. Devuelve el nombre del servicio, su versión, " +
        "la fecha y hora UTC del servidor y el estado de ejecución (Running o Stopping).")
    .Produces<DemoStatusResponse>(StatusCodes.Status503ServiceUnavailable);

app.Run();
