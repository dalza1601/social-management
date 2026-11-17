namespace SocialManagement.Beneficiarios.Api.Contracts.Common;

/// <summary>
/// Formato uniforme de las respuestas de los controllers que heredan de BaseApiController,
/// tanto de éxito como de error:
/// <code>{ "success": true, "data": { ... }, "message": null, "errors": [], "traceId": "00-..." }</code>
/// </summary>
/// <typeparam name="T">Tipo de los datos devueltos.</typeparam>
public sealed record ApiResponse<T>
{
    /// <summary>true si la operación se completó correctamente.</summary>
    public bool Success { get; init; }

    /// <summary>Datos de la respuesta; null cuando hay un error.</summary>
    public T? Data { get; init; }

    /// <summary>Mensaje para el usuario (opcional en éxitos, obligatorio en errores).</summary>
    public string? Message { get; init; }

    /// <summary>Errores de validación o de reglas de negocio; vacío en éxitos.</summary>
    public IReadOnlyList<ApiError> Errors { get; init; } = [];

    /// <summary>Identificador para correlacionar la respuesta con los logs.</summary>
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, string? traceId = null) =>
        new() { Success = true, Data = data, Message = message, TraceId = traceId };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<ApiError>? errors = null, string? traceId = null) =>
        new() { Success = false, Message = message, Errors = errors ?? [], TraceId = traceId };
}
