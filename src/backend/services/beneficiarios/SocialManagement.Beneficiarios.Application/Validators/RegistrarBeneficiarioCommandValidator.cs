using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

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
/// Las reglas de los datos personales están en <see cref="DatosPersonalesBeneficiarioValidator"/>
/// (compartidas con la actualización) y reutilizan las del dominio. Una prueba verifica que ambos
/// niveles aceptan y rechazan los mismos datos.
/// </remarks>
public sealed class RegistrarBeneficiarioCommandValidator : AbstractValidator<RegistrarBeneficiarioCommand>
{
    public RegistrarBeneficiarioCommandValidator() =>
        Include(new DatosPersonalesBeneficiarioValidator());
}
