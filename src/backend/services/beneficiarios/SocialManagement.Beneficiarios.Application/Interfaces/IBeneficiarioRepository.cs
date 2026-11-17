using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

namespace SocialManagement.Beneficiarios.Application.Interfaces;

/// <summary>
/// Puerto de persistencia de beneficiarios. La capa de aplicación DEFINE lo que necesita y la
/// infraestructura lo IMPLEMENTA (BeneficiarioRepository, con EF Core y SQL Server).
/// </summary>
/// <remarks>
/// Es una interfaz específica, no un IRepository&lt;T&gt; genérico: solo expone las operaciones
/// que usan los casos de uso. Contrato que toda implementación debe respetar:
/// - <see cref="ObtenerPorIdAsync"/> devuelve null si no existe (no lanza excepción). La entidad devuelta
///   queda bajo seguimiento: sus cambios se guardan con <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// - <see cref="AgregarAsync"/> solo REGISTRA al beneficiario como nuevo; se guarda cuando el caso de uso
///   confirma con <see cref="IUnitOfWork.SaveChangesAsync"/> (patrón Unit of Work).
/// - <see cref="ListarAsync"/> es de solo lectura: filtra por SearchTerm, ordena según Sort (por defecto apellidos y
///   nombres; el Id siempre desempata para que la paginación sea estable) y pagina con SafePageNumber/SafePageSize.
/// </remarks>
public interface IBeneficiarioRepository
{
    Task<Beneficiario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExisteConDocumentoAsync(DocumentoIdentidad documento, CancellationToken cancellationToken = default);

    Task<PagedResponse<Beneficiario>> ListarAsync(BeneficiarioQueryParameters parameters, CancellationToken cancellationToken = default);

    Task AgregarAsync(Beneficiario beneficiario, CancellationToken cancellationToken = default);
}
