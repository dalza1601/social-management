using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SocialManagement.Solicitudes.Domain.Exceptions;

namespace SocialManagement.Solicitudes.Api.IntegrationTests;

/// <summary>Verifica el manejo centralizado de excepciones (ProblemDetails).</summary>
[Trait("Category", "Integration")]
public sealed class ErrorHandlingTests(SolicitudesApiFactory factory) : IClassFixture<SolicitudesApiFactory>
{
    [Fact]
    public async Task DomainException_se_traduce_en_422_con_el_mensaje()
    {
        var (status, problem) = await HandleAsync(new TestDomainException("La regla X no se cumple."));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, status);
        Assert.Equal("La regla X no se cumple.", problem.GetProperty("detail").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task ValidationException_se_traduce_en_400_con_los_errores_por_campo()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Nombres", "Los nombres son obligatorios."),
            new ValidationFailure("Nombres", "Los nombres solo pueden contener letras."),
            new ValidationFailure("NumeroDocumento", "El DNI debe tener exactamente 8 dígitos."),
        ]);

        var (status, problem) = await HandleAsync(exception);

        // Formato estándar de ASP.NET Core para errores de validación (ValidationProblemDetails).
        Assert.Equal(StatusCodes.Status400BadRequest, status);
        var errors = problem.GetProperty("errors");
        Assert.Equal(2, errors.GetProperty("Nombres").GetArrayLength());
        Assert.Equal("El DNI debe tener exactamente 8 dígitos.", errors.GetProperty("NumeroDocumento")[0].GetString());
    }

    [Fact]
    public async Task Excepcion_inesperada_se_traduce_en_500_sin_exponer_detalles()
    {
        var (status, problem) = await HandleAsync(new InvalidOperationException("detalle interno secreto"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.DoesNotContain("secreto", problem.GetRawText(), StringComparison.Ordinal);
    }

    private async Task<(int Status, JsonElement Problem)> HandleAsync(Exception exception)
    {
        using var scope = factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetServices<IExceptionHandler>().Single();

        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = "GET";
        context.Request.Path = "/prueba";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);
        Assert.True(handled);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }

    private sealed class TestDomainException(string message) : DomainException(message);
}
