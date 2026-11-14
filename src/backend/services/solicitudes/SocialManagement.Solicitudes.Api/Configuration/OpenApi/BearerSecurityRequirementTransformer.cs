using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SocialManagement.Solicitudes.Api.Configuration.OpenApi;

/// <summary>
/// Marca como protegidas con Bearer solo las operaciones que requieren autorización
/// ([Authorize] sin [AllowAnonymous]) y documenta su respuesta 401.
/// </summary>
internal sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();

        if (!requiresAuthorization || !await BearerSecuritySchemeTransformer.IsBearerRegisteredAsync(context.ApplicationServices))
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = [],
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No autenticado: falta el token o no es válido." });
    }
}
