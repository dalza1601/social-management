using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SocialManagement.Solicitudes.Api.Configuration;
using SocialManagement.Solicitudes.Api.Configuration.OpenApi;
using SocialManagement.Solicitudes.Api.Middleware;

namespace SocialManagement.Solicitudes.Api.Extensions;

/// <summary>
/// Documentación de la API centralizada en la capa de presentación:
/// - Documento OpenAPI generado por Microsoft.AspNetCore.OpenApi: /openapi/v1.json
/// - Swagger UI (Swashbuckle.AspNetCore.SwaggerUI, solo la interfaz): /swagger
/// Domain, Application e Infrastructure no conocen nada de esto.
/// </summary>
public static partial class ApiDocumentationExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        const string section = ApiDocumentationOptions.SectionName;

        services.AddOptions<ApiDocumentationOptions>()
            .BindConfiguration(section)
            .Validate(
                o => RoutePrefixPattern().IsMatch(o.RoutePrefix ?? string.Empty),
                $"La configuración '{section}:RoutePrefix' no es válida: usa minúsculas, números y guiones, sin barras al inicio ni al final (p. ej. 'swagger').")
            .Validate(
                o => o.RoutePrefix != ApiDocumentationOptions.OpenApiRoutePrefix,
                $"La configuración '{section}:RoutePrefix' no puede ser '{ApiDocumentationOptions.OpenApiRoutePrefix}' (ruta reservada para el documento JSON).")
            .Validate(
                o => string.IsNullOrWhiteSpace(o.Access.UserName) == string.IsNullOrWhiteSpace(o.Access.Password),
                $"Define '{section}:Access:UserName' y '{section}:Access:Password' juntos, o ninguno de los dos.")
            .Validate(
                o => !o.Access.IsConfigured || o.Access.Password!.Length >= ApiDocumentationAccessOptions.MinimumPasswordLength,
                $"'{section}:Access:Password' debe tener al menos {ApiDocumentationAccessOptions.MinimumPasswordLength} caracteres.")
            .Validate<IHostEnvironment>(
                (o, environment) => !(o.Enabled && environment.IsProduction() && !o.Access.IsConfigured),
                $"En Production la documentación de la API solo puede habilitarse con control de acceso: define " +
                $"las variables de entorno '{section}__Access__UserName' y '{section}__Access__Password', " +
                $"o deshabilítala con '{section}__Enabled=false'.")
            .ValidateOnStart();

        services.AddOpenApi(ApiDocumentationOptions.DocumentName, options =>
        {
            options.AddDocumentTransformer<ServiceInfoDocumentTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
        });

        return services;
    }

    /// <summary>
    /// Publica el documento OpenAPI y Swagger UI si <c>ApiDocumentation:Enabled</c> es true.
    /// Debe llamarse antes de mapear los controllers.
    /// </summary>
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        if (!options.Enabled)
        {
            app.Logger.LogInformation("Documentación de la API deshabilitada (ApiDocumentation:Enabled = false)");
            return app;
        }

        var serviceName = app.Services.GetRequiredService<IOptions<ServiceInfoOptions>>().Value.Name;

        app.UseMiddleware<ApiDocumentationAccessMiddleware>();

        app.MapOpenApi($"/{ApiDocumentationOptions.OpenApiRoutePrefix}/{{documentName}}.json");

        app.UseSwaggerUI(ui =>
        {
            ui.RoutePrefix = options.RoutePrefix;
            // URL RELATIVA a la página de Swagger UI: funciona igual en http://localhost:8081/swagger
            // y detrás del Gateway en https://dominio/api/solicitudes/swagger.
            ui.SwaggerEndpoint(RelativeDocumentUrl(options), $"{serviceName} {ApiDocumentationOptions.DocumentName}");
            ui.DocumentTitle = $"SocialManagement · {serviceName}";
            ui.DisplayRequestDuration();
        });

        app.Logger.LogInformation(
            "Documentación de la API habilitada en /{SwaggerPath} y {OpenApiPath} (acceso protegido: {Protected})",
            options.RoutePrefix,
            options.OpenApiPath,
            options.Access.IsConfigured);

        return app;
    }

    internal static string RelativeDocumentUrl(ApiDocumentationOptions options)
    {
        var depth = options.RoutePrefix.Split('/').Length;
        return string.Concat(Enumerable.Repeat("../", depth)) + options.OpenApiPath.TrimStart('/');
    }

    [GeneratedRegex("^[a-z0-9]+(?:[-/][a-z0-9]+)*$")]
    private static partial Regex RoutePrefixPattern();
}
