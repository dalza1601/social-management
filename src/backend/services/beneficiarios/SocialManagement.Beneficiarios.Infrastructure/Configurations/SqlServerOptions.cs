namespace SocialManagement.Beneficiarios.Infrastructure.Configurations;

/// <summary>
/// Conexión a la base de datos propia de este microservicio (SocialManagement_Beneficiarios).
/// Se lee de la sección estándar <c>ConnectionStrings</c>, compatible con las herramientas de EF Core.
/// </summary>
public sealed class SqlServerOptions
{
    /// <summary>Nombre de la cadena de conexión: <c>ConnectionStrings:BeneficiariosDb</c>.</summary>
    public const string ConnectionStringName = "BeneficiariosDb";

    public const string ConfigurationKey = "ConnectionStrings:" + ConnectionStringName;

    public string ConnectionString { get; set; } = string.Empty;
}
