using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SocialManagement.Beneficiarios.Api.Contracts;

namespace SocialManagement.Beneficiarios.SqlServer.IntegrationTests;

/// <summary>Atajos HTTP para /api/v1/beneficiarios (mismo JSON que la API: camelCase y enums como texto).</summary>
internal static class BeneficiariosApiClient
{
    public const string Ruta = "/api/v1/beneficiarios";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Número de documento único por prueba (8 dígitos): las pruebas comparten la base de su clase.</summary>
    public static string NuevoDni() => Random.Shared.Next(10_000_000, 99_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static object Datos(string nombres, string apellidos, string numeroDocumento, string tipoDocumento = "Dni") =>
        new { nombres, apellidos, tipoDocumento, numeroDocumento };

    public static Task<HttpResponseMessage> RegistrarAsync(this HttpClient client, object datos) =>
        client.PostAsJsonAsync(Ruta, datos, Json, Ct);

    public static Task<HttpResponseMessage> ActualizarAsync(this HttpClient client, int id, object datos) =>
        client.PutAsJsonAsync($"{Ruta}/{id}", datos, Json, Ct);

    public static Task<HttpResponseMessage> DesactivarAsync(this HttpClient client, int id) =>
        client.PatchAsync($"{Ruta}/{id}/desactivar", content: null, Ct);

    public static async Task<ApiResponse<T>> LeerAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json, Ct)
        ?? throw new InvalidOperationException("La respuesta no tiene cuerpo.");
}
