using Microsoft.EntityFrameworkCore;
using SocialManagement.Solicitudes.Application.Interfaces;

namespace SocialManagement.Solicitudes.Infrastructure.Persistence;

/// <summary>
/// DbContext exclusivo del microservicio Solicitudes.
/// Ningún otro microservicio puede usarlo: cada servicio es dueño de su base de datos.
/// También es la implementación de <see cref="IUnitOfWork"/>: su SaveChangesAsync confirma en una sola
/// transacción todo lo que los repositorios registraron durante la petición.
/// </summary>
/// <remarks>
/// Aún no hay DbSet ni tablas. Al crear la primera entidad persistente:
/// 1) agrega su DbSet aquí, 2) crea su IEntityTypeConfiguration en Persistence/Configurations
/// (como BeneficiarioConfiguration en Beneficiarios) y descomenta ApplyConfigurationsFromAssembly,
/// 3) genera la migración (ver docs/infrastructure.md). La entidad del dominio no lleva atributos de persistencia.
/// </remarks>
public sealed class SolicitudesDbContext(DbContextOptions<SolicitudesDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>Esquema SQL donde se crearán las tablas de este servicio.</summary>
    public const string DefaultSchema = "solicitudes";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Al crear la primera IEntityTypeConfiguration, habilita el registro automático:
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(SolicitudesDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
