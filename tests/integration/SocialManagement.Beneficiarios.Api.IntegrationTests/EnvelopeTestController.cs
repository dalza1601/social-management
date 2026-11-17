using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Api.Contracts.Common;
using SocialManagement.Beneficiarios.Api.Controllers;

namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>
/// Controller SOLO de pruebas que hereda de <see cref="BaseApiController"/>: simula un controller de recursos
/// futuro para comprobar el formato <see cref="ApiResponse{T}"/> en éxitos y errores. Ruta: /api/envelopetest.
/// </summary>
public sealed class EnvelopeTestController : BaseApiController
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<ApiResponse<EnvelopeTestItem>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<EnvelopeTestItem>> ObtenerPorId(int id) =>
        ApiOkOrNotFound(id == 1 ? new EnvelopeTestItem(1, "Elemento de prueba") : null, $"No existe el elemento {id}.");

    [HttpPost]
    [ProducesResponseType<ApiResponse<EnvelopeTestItem>>(StatusCodes.Status201Created)]
    public ActionResult<ApiResponse<EnvelopeTestItem>> Crear(EnvelopeTestRequest request) =>
        ApiCreated(nameof(ObtenerPorId), new { id = 1 }, new EnvelopeTestItem(1, request.Nombre!), "Elemento creado.");

    /// <summary>Simula un caso de uso cuyo validador (FluentValidation) rechaza la entrada.</summary>
    [HttpPost("validacion-de-caso-de-uso")]
    public IActionResult ValidacionDeCasoDeUso() => throw new ValidationException(
    [
        new ValidationFailure("Nombres", "Los nombres son obligatorios."),
        new ValidationFailure("NumeroDocumento", "El DNI debe tener exactamente 8 dígitos."),
    ]);

    [HttpGet("regla-de-dominio")]
    public IActionResult ReglaDeDominio() => throw new TestRuleViolationException("La regla de prueba no se cumple.");

    [HttpGet("error-inesperado")]
    public IActionResult ErrorInesperado() => throw new InvalidOperationException("detalle interno secreto");
}
