using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de la entidad <see cref="Beneficiario"/> a la tabla <c>beneficiarios.Beneficiarios</c> (Fluent API de EF Core).
/// </summary>
/// <remarks>
/// Toda la información de persistencia vive aquí, no en el dominio: clave primaria, campos requeridos,
/// longitudes, conversiones, índices y relaciones. Así el dominio no depende de cómo se guarda.
/// <see cref="BeneficiariosDbContext"/> aplica esta clase con ApplyConfigurationsFromAssembly.
/// </remarks>
internal sealed class BeneficiarioConfiguration : IEntityTypeConfiguration<Beneficiario>
{
    public const string TableName = "Beneficiarios";
    public const string UniqueDocumentIndexName = "UX_Beneficiarios_Documento";

    // Los enums se guardan como texto: la base de datos es legible y no depende del valor numérico.
    private const int EnumMaxLength = 20;

    public void Configure(EntityTypeBuilder<Beneficiario> builder)
    {
        // El esquema "beneficiarios" lo define el DbContext (HasDefaultSchema).
        builder.ToTable(TableName);

        // Clave primaria entera autoincremental (IDENTITY(1,1)): la genera SQL Server al insertar y
        // EF Core la copia en Beneficiario.Id después del SaveChanges.
        builder.HasKey(beneficiario => beneficiario.Id);
        builder.Property(beneficiario => beneficiario.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        // Campos requeridos y longitudes: las mismas reglas que las invariantes del dominio.
        builder.Property(beneficiario => beneficiario.Nombres)
            .IsRequired()
            .HasMaxLength(Beneficiario.LongitudMaximaNombre);

        builder.Property(beneficiario => beneficiario.Apellidos)
            .IsRequired()
            .HasMaxLength(Beneficiario.LongitudMaximaNombre);

        builder.Property(beneficiario => beneficiario.FechaRegistroUtc)
            .IsRequired();

        builder.Property(beneficiario => beneficiario.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(EnumMaxLength);

        // Auditoría heredada de AuditableEntity (CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy).
        builder.ConfigureAuditFields();

        // Propiedades calculadas: no son columnas.
        builder.Ignore(beneficiario => beneficiario.NombreCompleto);
        builder.Ignore(beneficiario => beneficiario.EstaActivo);

        // Relación con el value object DocumentoIdentidad: "owned type". No tiene tabla ni identidad propia;
        // sus propiedades son columnas de la tabla Beneficiarios (DocumentoTipo, DocumentoNumero).
        builder.OwnsOne(beneficiario => beneficiario.Documento, documento =>
        {
            documento.Property(d => d.Tipo)
                .HasColumnName("DocumentoTipo")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(EnumMaxLength);

            documento.Property(d => d.Numero)
                .HasColumnName("DocumentoNumero")
                .IsRequired()
                .HasMaxLength(DocumentoIdentidad.LongitudMaximaNumero);

            // Garantía en la base de datos de la regla "un documento no puede repetirse"
            // (el caso de uso la valida antes; el índice protege ante registros simultáneos).
            documento.HasIndex(d => new { d.Tipo, d.Numero })
                .IsUnique()
                .HasDatabaseName(UniqueDocumentIndexName);
        });

        builder.Navigation(beneficiario => beneficiario.Documento)
            .IsRequired();

        // Relaciones con otras entidades del microservicio (cuando existan) se declaran aquí, por ejemplo:
        // builder.HasMany(b => b.Dependientes).WithOne().HasForeignKey("BeneficiarioId").OnDelete(DeleteBehavior.Cascade);
    }
}
