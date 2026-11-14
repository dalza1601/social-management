using System.ComponentModel.DataAnnotations;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>Solicitud de prueba con validaciones, para comprobar el formato de los errores 400.</summary>
public sealed class EnvelopeTestRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string? Nombre { get; init; }

    [Range(1, 10, ErrorMessage = "La cantidad debe estar entre 1 y 10.")]
    public int Cantidad { get; init; }
}
