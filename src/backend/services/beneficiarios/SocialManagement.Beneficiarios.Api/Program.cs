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

app.Run();
