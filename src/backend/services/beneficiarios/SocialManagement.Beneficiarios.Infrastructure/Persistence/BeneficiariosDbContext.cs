using Microsoft.EntityFrameworkCore;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence;

/// <summary>
/// DbContext exclusivo del microservicio Beneficiarios.
/// Ningún otro microservicio puede usarlo: cada servicio es dueño de su base de datos.
/// También es la implementación de <see cref="IUnitOfWork"/>: su SaveChangesAsync confirma en una sola
/// transacción todo lo que los repositorios registraron durante la petición.
/// </summary>
/// <remarks>
/// El mapeo de cada entidad (clave primaria, columnas, índices y relaciones) está en
/// Persistence/Configurations, una clase IEntityTypeConfiguration por entidad, aplicadas con
/// ApplyConfigurationsFromAssembly. Para una entidad nueva: agrega su DbSet aquí, crea su clase de
/// configuración y genera la migración (ver docs/infrastructure.md).
/// </remarks>
public sealed class BeneficiariosDbContext(DbContextOptions<BeneficiariosDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>Esquema SQL donde se crean las tablas de este servicio.</summary>
    public const string DefaultSchema = "beneficiarios";

    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Registra todas las IEntityTypeConfiguration de este ensamblado (p. ej. BeneficiarioConfiguration).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BeneficiariosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
