using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialManagement.Beneficiarios.Infrastructure.Configurations;

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence;

/// <summary>Aplicación de las migraciones de EF Core al iniciar la aplicación (si está habilitada).</summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Si <c>Persistence:ApplyMigrationsAtStartup</c> es true, aplica las migraciones pendientes de
    /// <see cref="BeneficiariosDbContext"/>. Es idempotente: las ya aplicadas no se repiten.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        if (!options.ApplyMigrationsAtStartup)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BeneficiariosDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrationExtensions));

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Base de datos al día: no hay migraciones pendientes");
            return;
        }

        logger.LogInformation("Aplicando {Count} migraciones: {Migrations}", pending.Count, string.Join(", ", pending));
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migraciones aplicadas correctamente");
    }
}
