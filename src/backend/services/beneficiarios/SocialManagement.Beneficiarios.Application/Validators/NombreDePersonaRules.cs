using FluentValidation;
using SocialManagement.Beneficiarios.Domain.Entities;

namespace SocialManagement.Beneficiarios.Application.Validators;

/// <summary>
/// Regla reutilizable de FluentValidation para nombres y apellidos:
/// <c>RuleFor(x => x.Nombres).NombreDePersona("nombres")</c>.
/// Usa las mismas reglas que la entidad <see cref="Beneficiario"/>.
/// </summary>
internal static class NombreDePersonaRules
{
    public static IRuleBuilderOptions<T, string> NombreDePersona<T>(this IRuleBuilderInitial<T, string> rule, string campo) =>
        rule
            .Cascade(CascadeMode.Stop)
            .Must(valor => Beneficiario.NormalizarNombre(valor).Length > 0)
            .WithMessage($"Los {campo} son obligatorios.")
            .Must(valor => Beneficiario.NormalizarNombre(valor).Length <= Beneficiario.LongitudMaximaNombre)
            .WithMessage($"Los {campo} no pueden superar {Beneficiario.LongitudMaximaNombre} caracteres.")
            .Must(valor => Beneficiario.TieneCaracteresPermitidos(Beneficiario.NormalizarNombre(valor)))
            .WithMessage($"Los {campo} solo pueden contener letras, espacios, apóstrofos, guiones y puntos.");
}
