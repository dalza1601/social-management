namespace SocialManagement.Solicitudes.Domain.Entities;

/// <summary>
/// Clase base de las entidades que registran quién y cuándo se crearon y modificaron.
/// </summary>
/// <remarks>
/// Los campos solo cambian mediante <see cref="MarkCreated"/> y <see cref="MarkUpdated"/> (setters privados).
/// La infraestructura los invoca AUTOMÁTICAMENTE al guardar (AuditableEntityInterceptor de EF Core),
/// así que los casos de uso no necesitan llamarlos.
/// - Al crear: CreatedAtUtc, CreatedBy (y UpdatedBy con el mismo usuario).
/// - Al modificar: UpdatedAtUtc y UpdatedBy (CreatedAtUtc y CreatedBy no cambian).
/// CreatedBy y UpdatedBy quedan en null hasta que exista autenticación (módulo de seguridad).
/// </remarks>
public abstract class AuditableEntity
{
    /// <summary>Momento de creación, en UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Usuario que creó el registro (null mientras no haya autenticación).</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>Momento de la última modificación, en UTC (null si nunca se modificó).</summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>Usuario que hizo la última modificación (null mientras no haya autenticación).</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>Registra la creación de la entidad.</summary>
    public void MarkCreated(DateTime createdAtUtc, string? createdBy)
    {
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        CreatedBy = createdBy;
        UpdatedBy = createdBy;
    }

    /// <summary>Registra una modificación de la entidad.</summary>
    public void MarkUpdated(DateTime? updatedAtUtc, string? updatedBy)
    {
        UpdatedAtUtc = updatedAtUtc.HasValue ? EnsureUtc(updatedAtUtc.Value) : null;
        UpdatedBy = updatedBy;
    }

    private static DateTime EnsureUtc(DateTime date) =>
        date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);
}
