using Microsoft.EntityFrameworkCore;

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence;

/// <summary>
/// DbContext exclusivo del microservicio Beneficiarios.
/// Ningún otro microservicio puede usarlo: cada servicio es dueño de su base de datos.
/// </summary>
/// <remarks>
/// Fase 0: no hay DbSet ni tablas. Al crear la primera entidad:
/// 1) agrega su DbSet aquí, 2) crea su IEntityTypeConfiguration en Persistence/EntityConfigurations
/// y descomenta ApplyConfigurationsFromAssembly, 3) genera la migración (ver docs/infrastructure.md).
/// </remarks>
public sealed class BeneficiariosDbContext(DbContextOptions<BeneficiariosDbContext> options)
    : DbContext(options)
{
    /// <summary>Esquema SQL donde se crearán las tablas de este servicio.</summary>
    public const string DefaultSchema = "beneficiarios";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Al crear la primera IEntityTypeConfiguration, habilita el registro automático:
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(BeneficiariosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
