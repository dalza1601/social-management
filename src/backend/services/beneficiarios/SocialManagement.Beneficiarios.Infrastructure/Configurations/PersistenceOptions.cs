namespace SocialManagement.Beneficiarios.Infrastructure.Configurations;

/// <summary>Opciones de persistencia (sección <c>Persistence</c>).</summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>
    /// Aplica las migraciones pendientes de EF Core al iniciar la API.
    /// Activado en Docker Compose (entorno local). En Production se recomienda dejarlo en false y aplicar
    /// las migraciones como un paso controlado del despliegue (script idempotente o migration bundle).
    /// </summary>
    public bool ApplyMigrationsAtStartup { get; set; }
}
