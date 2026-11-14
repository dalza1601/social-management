using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SocialManagement.Gateway.Health;

/// <summary>
/// Escribe el resultado de los health checks como JSON. El frontend Angular consume este formato.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // Mantiene legibles las tildes y eñes en la respuesta JSON.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static HealthCheckOptions CreateOptions(string tag) => new()
    {
        Predicate = registration => registration.Tags.Contains(tag),
        ResponseWriter = WriteAsync,
    };

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                error = entry.Value.Exception?.Message,
                data = entry.Value.Data,
            }),
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
