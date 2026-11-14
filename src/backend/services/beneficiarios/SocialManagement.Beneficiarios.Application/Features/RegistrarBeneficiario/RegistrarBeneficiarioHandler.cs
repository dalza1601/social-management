using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

/// <summary>
/// Caso de uso: registrar un beneficiario.
/// Orquesta los pasos; las reglas sobre los datos viven en el dominio.
/// </summary>
/// <remarks>
/// Depende solo de abstracciones (<see cref="IBeneficiarioRepository"/> y <see cref="TimeProvider"/>):
/// no sabe si los datos se guardan en SQL Server, en memoria o en una prueba.
/// </remarks>
public sealed class RegistrarBeneficiarioHandler(IBeneficiarioRepository repository, TimeProvider timeProvider)
{
    /// <exception cref="DocumentoIdentidadInvalidoException">El documento no tiene un formato válido.</exception>
    /// <exception cref="BeneficiarioInvalidoException">Los nombres o apellidos no son válidos.</exception>
    /// <exception cref="BeneficiarioDuplicadoException">Ya existe un beneficiario con ese documento.</exception>
    public async Task<BeneficiarioDto> HandleAsync(RegistrarBeneficiarioCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. El Value Object valida el documento.
        var documento = DocumentoIdentidad.Crear(command.TipoDocumento, command.NumeroDocumento);

        // 2. Regla que requiere consultar otros beneficiarios: el documento no puede repetirse.
        if (await repository.ExisteConDocumentoAsync(documento, cancellationToken))
        {
            throw new BeneficiarioDuplicadoException(documento);
        }

        // 3. La entidad valida sus invariantes al crearse.
        var beneficiario = Beneficiario.Registrar(command.Nombres, command.Apellidos, documento, timeProvider.GetUtcNow());

        // 4. Se guarda a través del puerto de persistencia.
        await repository.AgregarAsync(beneficiario, cancellationToken);

        return BeneficiarioDto.Desde(beneficiario);
    }
}
