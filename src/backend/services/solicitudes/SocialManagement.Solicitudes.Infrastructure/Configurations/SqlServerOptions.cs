namespace SocialManagement.Solicitudes.Infrastructure.Configurations;

/// <summary>
/// Conexión a la base de datos propia de este microservicio (SocialManagement_Solicitudes).
/// Se lee de la sección estándar <c>ConnectionStrings</c>, compatible con las herramientas de EF Core.
/// </summary>
public sealed class SqlServerOptions
{
    /// <summary>Nombre de la cadena de conexión: <c>ConnectionStrings:SolicitudesDb</c>.</summary>
    public const string ConnectionStringName = "SolicitudesDb";

    public const string ConfigurationKey = "ConnectionStrings:" + ConnectionStringName;

    public string ConnectionString { get; set; } = string.Empty;
}
