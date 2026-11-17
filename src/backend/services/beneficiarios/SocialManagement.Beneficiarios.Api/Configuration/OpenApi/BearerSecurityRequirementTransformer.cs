using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SocialManagement.Beneficiarios.Api.Configuration.OpenApi;

/// <summary>
/// Marca como protegidas con Bearer solo las operaciones que requieren autorización
/// ([Authorize] sin [AllowAnonymous]) y documenta su respuesta 401. Si la política exige roles,
/// documenta también la respuesta 403 y añade los roles permitidos a la descripción de la operación.
/// </summary>
internal sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        var requiresAuthorization = authorizeData.Count > 0 && !metadata.OfType<IAllowAnonymous>().Any();

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

        var roles = await AllowedRolesAsync(authorizeData, context.ApplicationServices);
        if (roles.Count == 0)
        {
            return;
        }

        var allowed = string.Join(", ", roles);
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = $"Sin permiso: el token no tiene ninguno de los roles {allowed}." });
        var note = $"**Roles permitidos:** {allowed}.";
        operation.Description = string.IsNullOrWhiteSpace(operation.Description) ? note : $"{operation.Description}\n\n{note}";
    }

    /// <summary>Roles exigidos por [Authorize(Roles = ...)] o por las políticas de [Authorize(Policy = ...)].</summary>
    private static async Task<IReadOnlyList<string>> AllowedRolesAsync(IEnumerable<IAuthorizeData> authorizeData, IServiceProvider services)
    {
        var policyProvider = services.GetService<IAuthorizationPolicyProvider>();
        var roles = new List<string>();

        foreach (var data in authorizeData)
        {
            if (!string.IsNullOrWhiteSpace(data.Roles))
            {
                roles.AddRange(data.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            if (policyProvider is not null && !string.IsNullOrWhiteSpace(data.Policy)
                && await policyProvider.GetPolicyAsync(data.Policy) is { } policy)
            {
                roles.AddRange(policy.Requirements.OfType<RolesAuthorizationRequirement>().SelectMany(requirement => requirement.AllowedRoles));
            }
        }

        return roles.Distinct(StringComparer.Ordinal).ToList();
    }
}
