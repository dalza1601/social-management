using Microsoft.AspNetCore.Mvc;
using SocialManagement.Beneficiarios.Api.Contracts;
using SocialManagement.Beneficiarios.Application.DTOs;
using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.DesactivarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;

namespace SocialManagement.Beneficiarios.Api.Controllers;

/// <summary>
/// Gestión de beneficiarios: /api/v1/beneficiarios.
/// Cada acción traduce HTTP ↔ caso de uso; no hay lógica de negocio ni de persistencia aquí.
/// La ruta, el formato ApiResponse y las respuestas 400/422/500 los aporta BaseApiController.
/// </summary>
public sealed class BeneficiariosController : BaseApiController
{
    /// <summary>Registra un beneficiario.</summary>
    /// <response code="201">Beneficiario registrado. La cabecera Location apunta a su consulta.</response>
    /// <response code="400">Datos de entrada no válidos (un error por campo).</response>
    /// <response code="409">Ya existe un beneficiario con el mismo documento.</response>
    [HttpPost]
    [ProducesResponseType<ApiResponse<BeneficiarioDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BeneficiarioDto>>> Registrar(
        RegistrarBeneficiarioRequest request,
        CancellationToken cancellationToken)
    {
        var beneficiario = await UseCase<RegistrarBeneficiarioHandler>().HandleAsync(request.ToCommand(), cancellationToken);
        return ApiCreated(nameof(ObtenerPorId), new { id = beneficiario.Id }, beneficiario, "Beneficiario registrado.");
    }

    /// <summary>Consulta un beneficiario por su identificador.</summary>
    /// <response code="200">Datos del beneficiario.</response>
    /// <response code="404">No existe un beneficiario con ese identificador.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ApiResponse<BeneficiarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BeneficiarioDto>>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var beneficiario = await UseCase<ConsultarBeneficiarioHandler>().HandleAsync(new ConsultarBeneficiarioQuery(id), cancellationToken);
        return ApiOkOrNotFound(beneficiario, NoExiste(id));
    }

    /// <summary>Lista los beneficiarios con paginación, ordenados por apellidos y nombres.</summary>
    /// <param name="pagina">Número de página, desde 1.</param>
    /// <param name="tamanoPagina">Registros por página, de 1 a 100.</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <response code="200">Página de resultados con el total de registros y de páginas.</response>
    /// <response code="400">Parámetros de paginación no válidos.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<BeneficiarioDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BeneficiarioDto>>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = ListarBeneficiariosQuery.TamanoPaginaPorDefecto,
        CancellationToken cancellationToken = default)
    {
        var resultado = await UseCase<ListarBeneficiariosHandler>().HandleAsync(new ListarBeneficiariosQuery(pagina, tamanoPagina), cancellationToken);
        return ApiOk(resultado);
    }

    /// <summary>Actualiza los datos personales de un beneficiario.</summary>
    /// <response code="200">Datos actualizados.</response>
    /// <response code="400">Datos de entrada no válidos.</response>
    /// <response code="404">No existe un beneficiario con ese identificador.</response>
    /// <response code="409">El nuevo documento ya pertenece a otro beneficiario.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ApiResponse<BeneficiarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BeneficiarioDto>>> Actualizar(
        int id,
        ActualizarBeneficiarioRequest request,
        CancellationToken cancellationToken)
    {
        var beneficiario = await UseCase<ActualizarBeneficiarioHandler>().HandleAsync(request.ToCommand(id), cancellationToken);
        return ApiOkOrNotFound(beneficiario, NoExiste(id));
    }

    /// <summary>Desactiva un beneficiario (baja lógica: el registro se conserva con estado Inactivo).</summary>
    /// <response code="200">Beneficiario desactivado (también si ya lo estaba).</response>
    /// <response code="404">No existe un beneficiario con ese identificador.</response>
    [HttpPatch("{id:int}/desactivar")]
    [ProducesResponseType<ApiResponse<BeneficiarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BeneficiarioDto>>> Desactivar(int id, CancellationToken cancellationToken)
    {
        var beneficiario = await UseCase<DesactivarBeneficiarioHandler>().HandleAsync(new DesactivarBeneficiarioCommand(id), cancellationToken);
        return ApiOkOrNotFound(beneficiario, NoExiste(id));
    }

    private static string NoExiste(int id) => $"No existe un beneficiario con el identificador {id}.";
}
