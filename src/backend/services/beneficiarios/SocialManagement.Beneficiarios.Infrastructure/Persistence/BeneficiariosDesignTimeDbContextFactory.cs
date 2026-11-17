using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence;

/// <summary>
/// Crea el DbContext para las herramientas de EF Core (<c>dotnet ef migrations ...</c>) sin arrancar la API
/// ni necesitar su configuración: las migraciones se generan igual en cualquier equipo (reproducibles).
/// </summary>
/// <remarks>
/// La cadena de conexión solo se usa en comandos que se conectan (<c>dotnet ef database update</c>):
/// variable de entorno <c>SM_BENEFICIARIOS_DB</c> o, por defecto, SQL Server LocalDB.
/// </remarks>
internal sealed class BeneficiariosDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BeneficiariosDbContext>
{
    public const string ConnectionStringVariable = "SM_BENEFICIARIOS_DB";

    private const string DefaultConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=SocialManagement_Beneficiarios;Integrated Security=True;TrustServerCertificate=True";

    public BeneficiariosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<BeneficiariosDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", BeneficiariosDbContext.DefaultSchema))
            .Options;

        return new BeneficiariosDbContext(options);
    }
}
