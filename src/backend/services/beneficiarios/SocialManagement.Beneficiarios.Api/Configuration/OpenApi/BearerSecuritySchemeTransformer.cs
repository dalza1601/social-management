using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SocialManagement.Beneficiarios.Api.Configuration.OpenApi;

/// <summary>
/// Declara el esquema de seguridad "Bearer" (JWT) en el documento OpenAPI, lo que habilita el botón
/// <b>Authorize</b> de Swagger UI. Solo actúa si la aplicación tiene registrado un esquema de
/// autenticación llamado "Bearer": desde la sesión 04 lo registra AddJwtBearer (ver SecurityExtensions).
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (!await IsBearerRegisteredAsync(context.ApplicationServices))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT. Pulsa Authorize y pega solo el token (sin el prefijo 'Bearer').",
        };
    }

    internal static async Task<bool> IsBearerRegisteredAsync(IServiceProvider services)
    {
        // GetService (no GetRequiredService): sin AddAuthentication() el proveedor no existe.
        var provider = services.GetService<IAuthenticationSchemeProvider>();
        if (provider is null)
        {
            return false;
        }

        var schemes = await provider.GetAllSchemesAsync();
        return schemes.Any(scheme => scheme.Name == SchemeName);
    }
}
