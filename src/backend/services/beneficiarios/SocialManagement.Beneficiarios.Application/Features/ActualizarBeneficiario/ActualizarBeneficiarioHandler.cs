using AutoMapper;
using FluentValidation;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

namespace SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;

/// <summary>Caso de uso: actualizar los datos personales de un beneficiario.</summary>
public sealed class ActualizarBeneficiarioHandler(
    IValidator<ActualizarBeneficiarioCommand> validator,
    IBeneficiarioRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
{
    /// <returns>Los datos actualizados, o <c>null</c> si el beneficiario no existe.</returns>
    /// <exception cref="ValidationException">Los datos de entrada no son válidos.</exception>
    /// <exception cref="BeneficiarioDuplicadoException">El nuevo documento ya pertenece a otro beneficiario.</exception>
    public async Task<BeneficiarioDto?> HandleAsync(ActualizarBeneficiarioCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var beneficiario = await repository.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (beneficiario is null)
        {
            return null;
        }

        var documento = DocumentoIdentidad.Crear(command.TipoDocumento, command.NumeroDocumento);

        // Si el documento cambia, no puede pertenecer a otro beneficiario.
        if (documento != beneficiario.Documento && await repository.ExisteConDocumentoAsync(documento, cancellationToken))
        {
            throw new BeneficiarioDuplicadoException(documento);
        }

        beneficiario.ActualizarDatos(command.Nombres, command.Apellidos, documento);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<BeneficiarioDto>(beneficiario);
    }
}
