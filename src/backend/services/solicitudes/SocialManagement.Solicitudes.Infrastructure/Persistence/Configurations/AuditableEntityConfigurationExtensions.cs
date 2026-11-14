using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialManagement.Solicitudes.Domain.Entities;

namespace SocialManagement.Solicitudes.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo común de los campos de auditoría de <see cref="AuditableEntity"/>.
/// Cada configuración de una entidad auditable lo invoca: <c>builder.ConfigureAuditFields();</c>
/// </summary>
internal static class AuditableEntityConfigurationExtensions
{
    public const int UserMaxLength = 100;

    public static EntityTypeBuilder<TEntity> ConfigureAuditFields<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        // SQL Server no guarda el "Kind" de DateTime: al leer, se marca como UTC para que se serialice con "Z".
        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired()
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(UserMaxLength);

        builder.Property(entity => entity.UpdatedAtUtc)
            .HasConversion(
                value => value,
                value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

        builder.Property(entity => entity.UpdatedBy)
            .HasMaxLength(UserMaxLength);

        return builder;
    }
}
