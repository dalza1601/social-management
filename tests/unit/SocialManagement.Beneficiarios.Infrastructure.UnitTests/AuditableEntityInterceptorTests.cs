using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;
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
    private readonly FakeCurrentUser _usuario = new();
    private readonly AuditableEntityInterceptor _interceptor;
    private readonly BeneficiariosDbContext _context = new(
        new DbContextOptionsBuilder<BeneficiariosDbContext>()
            .UseSqlServer("Server=localhost;Database=ModeloSinConexion;Trusted_Connection=True")
            .Options);

    public AuditableEntityInterceptorTests() => _interceptor = new AuditableEntityInterceptor(_clock, _usuario);

    private static Beneficiario NuevoBeneficiario() => Beneficiario.Registrar(
        "Ana", "Quispe", DocumentoIdentidad.Crear(TipoDocumentoIdentidad.Dni, "12345678"), Ahora);

    /// <summary>Beneficiario "leído de la base": con el Id que asignó SQL Server (el setter es privado).</summary>
    private static Beneficiario BeneficiarioGuardado(int id)
    {
        var beneficiario = NuevoBeneficiario();
        typeof(Beneficiario).GetProperty(nameof(Beneficiario.Id))!.SetValue(beneficiario, id);
        return beneficiario;
    }

    [Fact]
    public void Al_crear_completa_la_fecha_de_creacion_en_UTC()
    {
        var beneficiario = NuevoBeneficiario();
        _context.Add(beneficiario);

        _interceptor.ApplyAudit(_context);

        Assert.Equal(Ahora.UtcDateTime, beneficiario.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, beneficiario.CreatedAtUtc.Kind);
        Assert.Null(beneficiario.CreatedBy);        // sin usuario: p. ej. un proceso interno fuera de una petición HTTP
        Assert.Null(beneficiario.UpdatedAtUtc);
        Assert.Null(beneficiario.UpdatedBy);
    }

    [Fact]
    public void Al_modificar_completa_la_fecha_de_modificacion_y_protege_la_de_creacion()
    {
        // Simula un registro ya guardado: Id asignado por la base y auditoría de creación completa.
        var beneficiario = BeneficiarioGuardado(1);
        beneficiario.MarkCreated(Ahora.UtcDateTime, createdBy: null);
        _context.Attach(beneficiario);   // Unchanged

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
        var beneficiario = BeneficiarioGuardado(1);
        _context.Attach(beneficiario);   // Unchanged (con Id 0, EF Core lo trataría como nuevo: Added)

        _interceptor.ApplyAudit(_context);

        Assert.Equal(default, beneficiario.CreatedAtUtc);
        Assert.Null(beneficiario.UpdatedAtUtc);
    }

    [Fact]
    public void Al_crear_registra_el_usuario_autenticado()
    {
        _usuario.UserName = "operador.demo";
        var beneficiario = NuevoBeneficiario();
        _context.Add(beneficiario);

        _interceptor.ApplyAudit(_context);

        Assert.Equal("operador.demo", beneficiario.CreatedBy);
        Assert.Equal("operador.demo", beneficiario.UpdatedBy);   // AuditableEntity.MarkCreated también lo asigna
        Assert.Null(beneficiario.UpdatedAtUtc);
    }

    [Fact]
    public void Al_modificar_registra_quien_modifico_y_conserva_quien_creo()
    {
        var beneficiario = BeneficiarioGuardado(1);
        beneficiario.MarkCreated(Ahora.UtcDateTime, createdBy: "operador.demo");
        _context.Attach(beneficiario);

        _usuario.UserName = "admin.demo";   // el usuario que hace la petición actual (sale del token JWT)
        beneficiario.Desactivar();
        _context.ChangeTracker.DetectChanges();
        _interceptor.ApplyAudit(_context);

        Assert.Equal("operador.demo", beneficiario.CreatedBy);
        Assert.Equal("admin.demo", beneficiario.UpdatedBy);
    }

    public void Dispose() => _context.Dispose();
}
