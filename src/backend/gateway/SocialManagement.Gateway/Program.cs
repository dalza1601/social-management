using SocialManagement.Gateway.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Rutas y destinos se leen de la sección "ReverseProxy" (appsettings.json).
// En Docker, las direcciones de los destinos se sobrescriben con variables de entorno, p. ej.:
// ReverseProxy__Clusters__beneficiarios__Destinations__default__Address=http://beneficiarios-api:8080/
var reverseProxySection = builder.Configuration.GetSection("ReverseProxy");
builder.Services.AddReverseProxy().LoadFromConfig(reverseProxySection);

builder.Services.AddProblemDetails();
builder.Services.AddGatewayHealthChecks(reverseProxySection);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// /health       -> el Gateway responde.
// /health/ready -> el Gateway alcanza el /health de cada microservicio.
app.MapHealthChecks("/health", HealthCheckResponseWriter.CreateOptions(GatewayHealthChecks.LivenessTag));
app.MapHealthChecks("/health/ready", HealthCheckResponseWriter.CreateOptions(GatewayHealthChecks.ReadinessTag));

app.MapReverseProxy();

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Gateway iniciado en el entorno {Environment}", app.Environment.EnvironmentName));

app.Run();
