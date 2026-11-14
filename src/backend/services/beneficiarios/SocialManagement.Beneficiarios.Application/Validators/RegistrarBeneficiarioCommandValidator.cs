using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>
/// Valida los datos de entrada del registro ANTES de convertirlos en objetos de dominio.
/// Devuelve TODOS los errores a la vez, por campo, para que el cliente (formulario Angular) los muestre juntos.
/// </summary>
/// <remarks>
/// Dos niveles de validación:
/// 1. Este validador (Application): formato de la entrada, con mensajes por campo.
/// 2. Las invariantes del dominio (Beneficiario, DocumentoIdentidad): la última barrera; un objeto
///    de dominio nunca existe en estado inválido.
/// Para no duplicar reglas, el validador reutiliza las del dominio (LongitudMaximaNombre,
/// NormalizarNombre, TieneCaracteresPermitidos, DocumentoIdentidad.EsValido). Una prueba
/// verifica que ambos niveles aceptan y rechazan los mismos datos.
/// </remarks>
public sealed class RegistrarBeneficiarioCommandValidator : AbstractValidator<RegistrarBeneficiarioCommand>
{
    public RegistrarBeneficiarioCommandValidator()
    {
        RuleFor(command => command.Nombres).NombreDePersona("nombres");
        RuleFor(command => command.Apellidos).NombreDePersona("apellidos");

        RuleFor(command => command.TipoDocumento)
            .IsInEnum()
            .WithMessage("El tipo de documento no es válido.");

        RuleFor(command => command.NumeroDocumento)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("El número de documento es obligatorio.")
            .Must((command, numero) => DocumentoIdentidad.EsValido(command.TipoDocumento, numero))
            .WithMessage(command => DocumentoIdentidad.DescripcionDelFormato(command.TipoDocumento))
            .When(command => Enum.IsDefined(command.TipoDocumento), ApplyConditionTo.CurrentValidator);
    }
}
