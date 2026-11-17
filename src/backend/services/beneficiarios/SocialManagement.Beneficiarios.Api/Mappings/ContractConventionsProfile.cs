using AutoMapper;

namespace SocialManagement.Beneficiarios.Api.Mappings;

/// <summary>
/// Reglas de mapeo comunes a todos los contratos HTTP de la API.
/// </summary>
/// <remarks>
/// Los campos de texto de los requests son opcionales (<c>string?</c>) para que FluentValidation informe
/// "es obligatorio" campo por campo. Al pasar al comando, un texto ausente llega como cadena vacía (nunca null):
/// así lo rechazan los validadores con su mensaje y no con un error inesperado.
/// </remarks>
public sealed class ContractConventionsProfile : Profile
{
    public ContractConventionsProfile()
    {
        CreateMap<string?, string>().ConvertUsing(valor => valor ?? string.Empty);
    }
}
