using FluentValidation;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Domain.ValueObjects.Beneficiarios;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>
/// Reglas de los datos personales, comunes a registrar y actualizar (se incluyen con <c>Include(...)</c>).
/// Reutilizan las reglas del dominio para no duplicarlas.
/// </summary>
public sealed class DatosPersonalesBeneficiarioValidator : AbstractValidator<IDatosPersonalesBeneficiario>
{
    public DatosPersonalesBeneficiarioValidator()
    {
        RuleFor(datos => datos.Nombres).NombreDePersona("nombres");
        RuleFor(datos => datos.Apellidos).NombreDePersona("apellidos");

        RuleFor(datos => datos.TipoDocumento)
            .IsInEnum()
            .WithMessage("El tipo de documento no es válido.");

        RuleFor(datos => datos.NumeroDocumento)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("El número de documento es obligatorio.")
            .Must((datos, numero) => DocumentoIdentidad.EsValido(datos.TipoDocumento, numero))
            .WithMessage(datos => DocumentoIdentidad.DescripcionDelFormato(datos.TipoDocumento))
            .When(datos => Enum.IsDefined(datos.TipoDocumento), ApplyConditionTo.CurrentValidator);
    }
}
