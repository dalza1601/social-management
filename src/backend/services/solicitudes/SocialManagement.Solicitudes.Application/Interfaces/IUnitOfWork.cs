namespace SocialManagement.Solicitudes.Application.Interfaces;

/// <summary>
/// Unidad de trabajo: confirma en una sola operación todos los cambios registrados por los
/// repositorios durante un caso de uso (todo o nada).
/// </summary>
/// <remarks>
/// Lo usan los casos de uso (handlers), nunca los controllers. La implementación es el DbContext
/// del microservicio (Infrastructure), compartido con los repositorios dentro de la misma petición.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Guarda los cambios pendientes y devuelve el número de registros afectados.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
