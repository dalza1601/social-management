namespace SocialManagement.Solicitudes.Api.Contracts;

/// <summary>Un error dentro de <see cref="ApiResponse{T}"/>.</summary>
/// <param name="Field">Campo de la solicitud al que se refiere el error (null si es general).</param>
/// <param name="Message">Descripción comprensible del error.</param>
public sealed record ApiError(string? Field, string Message);
