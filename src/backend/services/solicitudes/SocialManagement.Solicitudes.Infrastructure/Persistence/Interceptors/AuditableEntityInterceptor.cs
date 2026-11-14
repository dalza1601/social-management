using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SocialManagement.Solicitudes.Domain.Entities;

namespace SocialManagement.Solicitudes.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Completa los campos de <see cref="AuditableEntity"/> justo antes de cada SaveChanges, para todas las
/// entidades auditables: nadie tiene que acordarse de llenarlos.
/// </summary>
/// <remarks>
/// - Entidad nueva (Added): <see cref="AuditableEntity.MarkCreated"/> con la hora y el usuario actuales.
/// - Entidad modificada (Modified): <see cref="AuditableEntity.MarkUpdated"/>; CreatedAtUtc y CreatedBy
///   se protegen para que no se sobrescriban.
/// La hora viene de <see cref="TimeProvider"/> (reemplazable en pruebas). El usuario actual será null
/// hasta que el proyecto incorpore autenticación (módulo de seguridad).
/// </remarks>
internal sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    internal void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        string? currentUser = null;   // Con autenticación: el usuario del token (p. ej. un ICurrentUser).

        // Detecta los cambios hechos con métodos del dominio (p. ej. Desactivar()) antes de revisar los estados.
        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkCreated(now, currentUser);
                    break;

                case EntityState.Modified:
                    entry.Entity.MarkUpdated(now, currentUser);
                    // La creación nunca se reescribe al actualizar.
                    entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                    entry.Property(entity => entity.CreatedBy).IsModified = false;
                    break;
            }
        }
    }
}
