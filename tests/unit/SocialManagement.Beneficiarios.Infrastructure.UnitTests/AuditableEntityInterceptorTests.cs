using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.ValueObjects;
using SocialManagement.Beneficiarios.Infrastructure.Persistence;
using SocialManagement.Beneficiarios.Infrastructure.Persistence.Interceptors;

namespace SocialManagement.Beneficiarios.Infrastructure.UnitTests;

/// <summary>
/// El interceptor completa la auditoría de las entidades antes de guardar.
/// Se prueba sobre el ChangeTracker de EF Core: no se necesita SQL Server.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AuditableEntityInterceptorTests : IDisposable
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Ahora);
    private readonly AuditableEntityInterceptor _interceptor;
    private readonly BeneficiariosDbContext _context = new(
        new DbContextOptionsBuilder<BeneficiariosDbContext>()
            .UseSqlServer("Server=localhost;Database=ModeloSinConexion;Trusted_Connection=True")
            .Options);

    public AuditableEntityInterceptorTests() => _interceptor = new AuditableEntityInterceptor(_clock);

    private static Beneficiario NuevoBeneficiario() => Beneficiario.Registrar(
        "Ana", "Quispe", DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, "12345678"), Ahora);

    [Fact]
    public void Al_crear_completa_la_fecha_de_creacion_en_UTC()
    {
        var beneficiario = NuevoBeneficiario();
        _context.Add(beneficiario);

        _interceptor.ApplyAudit(_context);

        Assert.Equal(Ahora.UtcDateTime, beneficiario.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, beneficiario.CreatedAtUtc.Kind);
        Assert.Null(beneficiario.CreatedBy);        // sin autenticación todavía
        Assert.Null(beneficiario.UpdatedAtUtc);
        Assert.Null(beneficiario.UpdatedBy);
    }

    [Fact]
    public void Al_modificar_completa_la_fecha_de_modificacion_y_protege_la_de_creacion()
    {
        var beneficiario = NuevoBeneficiario();
        _context.Add(beneficiario);
        _interceptor.ApplyAudit(_context);
        _context.Entry(beneficiario).State = EntityState.Unchanged;   // simula que ya estaba guardado

        _clock.Advance(TimeSpan.FromHours(2));
        beneficiario.Desactivar();
        _context.ChangeTracker.DetectChanges();
        _interceptor.ApplyAudit(_context);

        var entry = _context.Entry(beneficiario);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal(Ahora.UtcDateTime.AddHours(2), beneficiario.UpdatedAtUtc);
        Assert.Equal(Ahora.UtcDateTime, beneficiario.CreatedAtUtc);
        Assert.False(entry.Property(b => b.CreatedAtUtc).IsModified);
        Assert.False(entry.Property(b => b.CreatedBy).IsModified);
    }

    [Fact]
    public void Las_entidades_sin_cambios_no_se_tocan()
    {
        var beneficiario = NuevoBeneficiario();
        _context.Attach(beneficiario);   // Unchanged

        _interceptor.ApplyAudit(_context);

        Assert.Equal(default, beneficiario.CreatedAtUtc);
        Assert.Null(beneficiario.UpdatedAtUtc);
    }

    public void Dispose() => _context.Dispose();
}
