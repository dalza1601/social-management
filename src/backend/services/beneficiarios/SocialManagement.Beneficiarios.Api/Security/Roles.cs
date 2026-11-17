namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>
/// Roles del sistema (sesión 04). Viajan en el token JWT en el claim <c>role</c>.
/// </summary>
/// <remarks>
/// | Rol           | Puede                                                       |
/// | Operador      | Consultar, listar, registrar y actualizar beneficiarios     |
/// | Administrador | Todo lo anterior y además desactivar (dar de baja)          |
/// Qué rol exige cada operación lo deciden las políticas de <see cref="AuthorizationPolicies"/>, no los controllers.
/// </remarks>
public static class Roles
{
    public const string Administrador = "Administrador";

    public const string Operador = "Operador";

    /// <summary>Todos los roles válidos (se usa para validar la configuración de los usuarios demo).</summary>
    public static IReadOnlyList<string> Todos { get; } = [Administrador, Operador];
}
