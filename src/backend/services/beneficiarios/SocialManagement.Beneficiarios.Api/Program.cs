using SocialManagement.Beneficiarios.Api;
using SocialManagement.Beneficiarios.Api.Contracts.Diagnostics;
using SocialManagement.Beneficiarios.Api.Endpoints;
using SocialManagement.Beneficiarios.Api.Extensions;
using SocialManagement.Beneficiarios.Application;
using SocialManagement.Beneficiarios.Infrastructure;
using SocialManagement.Beneficiarios.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Valida el grafo de dependencias al construir la aplicación (falla rápido si falta un registro).
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Composición por capas: cada capa de Clean Architecture tiene su DependencyInjection/DependencyInjection.cs con un único método.
// (Domain no tiene: no registra servicios.)
builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddPresentation();

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

// Sesión 04: emisión de tokens JWT SOLO en Development (DevelopmentTokens:Enabled). En Staging y Production
// esta ruta no existe; los tokens los emitiría un proveedor de identidad real.
if (app.IsDevelopmentTokenIssuerEnabled())
{
    app.MapPost(IssueDevelopmentTokenEndpoint.Route, IssueDevelopmentTokenEndpoint.Handle)
        .WithName("IssueDevelopmentToken")
        .WithTags("Autenticación (solo desarrollo)")
        .WithSummary("Emite un token JWT para un usuario de demostración (solo Development).")
        .WithDescription(
            "Cambia el usuario y la clave de un usuario demo (configurados en user-secrets o variables de entorno) " +
            "por un token JWT con sus roles. Copia el valor de accessToken y pégalo en el botón Authorize. " +
            "No es un proveedor de identidad: no hay refresh tokens ni registro de usuarios.")
        .AllowAnonymous();
}

// Aplica las migraciones de EF Core pendientes si Persistence:ApplyMigrationsAtStartup = true (Docker Compose).
await app.Services.ApplyDatabaseMigrationsAsync();

app.Run();
