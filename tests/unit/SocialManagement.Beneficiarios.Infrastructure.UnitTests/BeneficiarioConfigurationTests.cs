using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects;
using SocialManagement.Beneficiarios.Infrastructure.Persistence;

namespace SocialManagement.Beneficiarios.Infrastructure.UnitTests;

/// <summary>
/// Mapeo de <see cref="Beneficiario"/> definido en Persistence/Configurations/BeneficiarioConfiguration.cs.
/// El modelo de EF Core se construye en memoria: no se necesita SQL Server.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BeneficiarioConfigurationTests : IDisposable
{
    private readonly BeneficiariosDbContext _context = new(
        new DbContextOptionsBuilder<BeneficiariosDbContext>()
            .UseSqlServer("Server=localhost;Database=ModeloSinConexion;Trusted_Connection=True")
            .Options);

    private IEntityType Entidad =>
        _context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Beneficiario))!;

    [Fact]
    public void Se_guarda_en_la_tabla_Beneficiarios_del_esquema_del_servicio()
    {
        Assert.Equal("Beneficiarios", Entidad.GetTableName());
        Assert.Equal("beneficiarios", Entidad.GetSchema());
    }

    [Fact]
    public void Id_es_la_clave_primaria_entera_y_la_genera_SQL_Server()
    {
        var key = Entidad.FindPrimaryKey()!;

        var property = Assert.Single(key.Properties);
        Assert.Equal(nameof(Beneficiario.Id), property.Name);
        Assert.Equal(typeof(int), property.ClrType);
        Assert.Equal(ValueGenerated.OnAdd, property.ValueGenerated);
        Assert.Equal(SqlServerValueGenerationStrategy.IdentityColumn, property.GetValueGenerationStrategy());
    }

    [Theory]
    [InlineData(nameof(Beneficiario.Nombres), Beneficiario.LongitudMaximaNombre)]
    [InlineData(nameof(Beneficiario.Apellidos), Beneficiario.LongitudMaximaNombre)]
    [InlineData(nameof(Beneficiario.Estado), 20)]
    public void Los_campos_son_requeridos_y_con_longitud_maxima(string propiedad, int longitud)
    {
        var property = Entidad.FindProperty(propiedad)!;

        Assert.False(property.IsNullable);
        Assert.Equal(longitud, property.GetMaxLength());
    }

    [Fact]
    public void La_fecha_de_registro_es_requerida()
    {
        Assert.False(Entidad.FindProperty(nameof(Beneficiario.FechaRegistroUtc))!.IsNullable);
    }

    [Theory]
    [InlineData(nameof(AuditableEntity.CreatedAtUtc), false, null)]
    [InlineData(nameof(AuditableEntity.CreatedBy), true, 100)]
    [InlineData(nameof(AuditableEntity.UpdatedAtUtc), true, null)]
    [InlineData(nameof(AuditableEntity.UpdatedBy), true, 100)]
    public void Los_campos_de_auditoria_heredados_son_columnas(string propiedad, bool admiteNulo, int? longitud)
    {
        var property = Entidad.FindProperty(propiedad)!;

        Assert.NotNull(property);
        Assert.Equal(admiteNulo, property.IsNullable);
        Assert.Equal(longitud, property.GetMaxLength());
    }

    [Fact]
    public void El_estado_se_guarda_como_texto()
    {
        Assert.Equal(typeof(string), Entidad.FindProperty(nameof(Beneficiario.Estado))!.GetProviderClrType());
    }

    [Fact]
    public void Las_propiedades_calculadas_no_son_columnas()
    {
        Assert.Null(Entidad.FindProperty(nameof(Beneficiario.NombreCompleto)));
        Assert.Null(Entidad.FindProperty(nameof(Beneficiario.EstaActivo)));
    }

    [Fact]
    public void El_documento_es_un_value_object_propio_con_indice_unico()
    {
        var navigation = Entidad.FindNavigation(nameof(Beneficiario.Documento))!;
        var documento = navigation.TargetEntityType;

        Assert.True(navigation.ForeignKey.IsOwnership);
        Assert.Equal("Beneficiarios", documento.GetTableName());   // mismas columnas de la tabla Beneficiarios
        Assert.Equal("DocumentoTipo", documento.FindProperty(nameof(DocumentoIdentidad.Tipo))!.GetColumnName());
        Assert.Equal("DocumentoNumero", documento.FindProperty(nameof(DocumentoIdentidad.Numero))!.GetColumnName());
        Assert.Equal(DocumentoIdentidad.LongitudMaximaNumero, documento.FindProperty(nameof(DocumentoIdentidad.Numero))!.GetMaxLength());

        var index = Assert.Single(documento.GetIndexes());
        Assert.True(index.IsUnique);
        Assert.Equal("UX_Beneficiarios_Documento", index.GetDatabaseName());
    }

    [Fact]
    public void El_script_SQL_crea_la_tabla_con_clave_primaria_e_indice_unico()
    {
        var script = _context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE [beneficiarios].[Beneficiarios]", script, StringComparison.Ordinal);
        Assert.Contains("[Id] int NOT NULL IDENTITY", script, StringComparison.Ordinal);
        Assert.Contains("[Nombres] nvarchar(100) NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("[DocumentoNumero] nvarchar(12) NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("[CreatedAtUtc] datetime2 NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("[CreatedBy] nvarchar(100) NULL", script, StringComparison.Ordinal);
        Assert.Contains("[UpdatedAtUtc] datetime2 NULL", script, StringComparison.Ordinal);
        Assert.Contains("[UpdatedBy] nvarchar(100) NULL", script, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT [PK_Beneficiarios] PRIMARY KEY ([Id])", script, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX [UX_Beneficiarios_Documento]", script, StringComparison.Ordinal);
    }

    public void Dispose() => _context.Dispose();
}
