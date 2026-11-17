using Microsoft.AspNetCore.Authorization;

namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Políticas de autorización de la API de beneficiarios (sesión 04). Cada acción del controller declara
/// EXPLÍCITAMENTE su política con <c>[Authorize(Policy = ...)]</c>, también las de lectura.
/// </summary>
/// <remarks>
/// | Política                       | Roles                    | Operaciones                         |
/// | Beneficiarios.Lectura          | Administrador, Operador  | GET por id, GET listado             |
/// | Beneficiarios.Escritura        | Administrador, Operador  | POST registrar, PUT actualizar      |
/// | Beneficiarios.Desactivacion    | Administrador            | PATCH desactivar                    |
/// Una política (y no <c>[Authorize(Roles = "...")]</c> en cada acción) permite cambiar quién puede hacer qué
/// en un solo lugar, sin tocar los controllers.
/// </remarks>
public static class AuthorizationPolicies
{
    public const string LecturaBeneficiarios = "Beneficiarios.Lectura";

    public const string EscrituraBeneficiarios = "Beneficiarios.Escritura";

    public const string DesactivacionBeneficiarios = "Beneficiarios.Desactivacion";

    /// <summary>Registra las políticas. Todas exigen un usuario autenticado y alguno de los roles indicados.</summary>
    public static AuthorizationBuilder AddBeneficiariosPolicies(this AuthorizationBuilder authorization) =>
        authorization
            .AddPolicy(LecturaBeneficiarios, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Administrador, Roles.Operador))
            .AddPolicy(EscrituraBeneficiarios, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Administrador, Roles.Operador))
            .AddPolicy(DesactivacionBeneficiarios, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Administrador));
}
