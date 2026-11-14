using SocialManagement.Solicitudes.Api.Endpoints;
using SocialManagement.Solicitudes.Api.Extensions;
using SocialManagement.Solicitudes.Application;
using SocialManagement.Solicitudes.Infrastructure.DependencyInjection;

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

app.Run();
