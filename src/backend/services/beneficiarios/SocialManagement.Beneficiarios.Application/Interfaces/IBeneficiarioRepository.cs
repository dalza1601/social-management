using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.Interfaces;

/// <summary>
/// Puerto de persistencia de beneficiarios. La capa de aplicación DEFINE lo que necesita y la
/// infraestructura lo IMPLEMENTARÁ (con EF Core y SQL Server en una sesión posterior).
/// </summary>
/// <remarks>
/// Es una interfaz específica, no un IRepository&lt;T&gt; genérico: solo expone las operaciones
/// que usan los casos de uso actuales. Se ampliará cuando un caso de uso nuevo lo necesite.
/// Contrato que toda implementación debe respetar:
/// - <see cref="ObtenerPorIdAsync"/> devuelve null si no existe (no lanza excepción).
/// - <see cref="AgregarAsync"/> solo REGISTRA al beneficiario como nuevo; se guarda cuando el caso de uso
///   confirma con <see cref="IUnitOfWork.SaveChangesAsync"/> (patrón Unit of Work).
/// </remarks>
public interface IBeneficiarioRepository
{
    Task<Beneficiario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExisteConDocumentoAsync(DocumentoIdentidad documento, CancellationToken cancellationToken = default);

    Task AgregarAsync(Beneficiario beneficiario, CancellationToken cancellationToken = default);
}
