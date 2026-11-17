using AutoMapper;
using FluentValidation;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

namespace SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

/// <summary>
/// Caso de uso: registrar un beneficiario.
/// Orquesta los pasos; las reglas sobre los datos viven en el dominio.
/// </summary>
/// <remarks>
/// Depende solo de abstracciones (<see cref="IBeneficiarioRepository"/>, <see cref="IUnitOfWork"/>, IMapper y
/// <see cref="TimeProvider"/>): no sabe si los datos se guardan en SQL Server, en memoria o en una prueba.
/// </remarks>
public sealed class RegistrarBeneficiarioHandler(
    IValidator<RegistrarBeneficiarioCommand> validator,
    IBeneficiarioRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IMapper mapper)
{
    /// <exception cref="ValidationException">Los datos de entrada no son válidos (se informan todos los campos).</exception>
    /// <exception cref="BeneficiarioDuplicadoException">Ya existe un beneficiario con ese documento.</exception>
    public async Task<BeneficiarioDto> HandleAsync(RegistrarBeneficiarioCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 0. Validación de la entrada (FluentValidation): informa TODOS los errores por campo.
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        // 1. Conversión a dominio: el Value Object vuelve a garantizar su invariante.
        var documento = DocumentoIdentidad.Crear(command.TipoDocumento, command.NumeroDocumento);

        // 2. Regla que requiere consultar otros beneficiarios: el documento no puede repetirse.
        if (await repository.ExisteConDocumentoAsync(documento, cancellationToken))
        {
            throw new BeneficiarioDuplicadoException(documento);
        }

        // 3. La entidad valida sus invariantes al crearse.
        var beneficiario = Beneficiario.Registrar(command.Nombres, command.Apellidos, documento, timeProvider.GetUtcNow());

        // 4. Se registra en el repositorio y se confirma con la unidad de trabajo (todo o nada).
        await repository.AgregarAsync(beneficiario, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. El DTO se arma DESPUÉS de guardar: el Id (IDENTITY) lo asigna la base de datos en el INSERT.
        return mapper.Map<BeneficiarioDto>(beneficiario);
    }
}
