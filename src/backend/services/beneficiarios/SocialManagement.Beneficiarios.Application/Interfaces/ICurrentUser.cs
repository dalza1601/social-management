namespace SocialManagement.Beneficiarios.Application.Interfaces;

/// <summary>
/// Usuario que ejecuta la operación actual (sesión 04). Application e Infrastructure lo usan sin saber
/// de dónde sale: en la API lo aporta el token JWT de la petición; fuera de una petición HTTP es null.
/// </summary>
/// <remarks>
/// Uso principal: los campos de auditoría CreatedBy / UpdatedBy (AuditableEntityInterceptor).
/// </remarks>
public interface ICurrentUser
{
    /// <summary>Nombre del usuario autenticado, o null si no hay un usuario (proceso interno, migración...).</summary>
    string? UserName { get; }
}
