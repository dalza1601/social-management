using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class RegistrarBeneficiarioHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly RegistrarBeneficiarioHandler _handler;

    public RegistrarBeneficiarioHandlerTests() =>
        _handler = new RegistrarBeneficiarioHandler(_repository, new FakeTimeProvider(Ahora));

    private static RegistrarBeneficiarioCommand ComandoValido(string dni = "12345678") =>
        new("Ana María", "Quispe Mamani", TipoDocumentoIdentidad.Dni, dni);

    [Fact]
    public async Task Registra_y_guarda_un_beneficiario_activo()
    {
        var resultado = await _handler.HandleAsync(ComandoValido(), TestContext.Current.CancellationToken);

        var guardado = Assert.Single(_repository.Guardados);
        Assert.Equal(guardado.Id, resultado.Id);
        Assert.Equal("Ana María Quispe Mamani", resultado.NombreCompleto);
        Assert.Equal("Dni", resultado.TipoDocumento);
        Assert.Equal("12345678", resultado.NumeroDocumento);
        Assert.Equal("Activo", resultado.Estado);
    }

    [Fact]
    public async Task La_fecha_de_registro_proviene_del_TimeProvider()
    {
        var resultado = await _handler.HandleAsync(ComandoValido(), TestContext.Current.CancellationToken);

        Assert.Equal(Ahora, resultado.FechaRegistroUtc);
    }

    [Fact]
    public async Task Rechaza_un_documento_ya_registrado_y_no_guarda_el_duplicado()
    {
        await _handler.HandleAsync(ComandoValido("12345678"), TestContext.Current.CancellationToken);

        // Mismo documento escrito con espacios: el Value Object lo normaliza y lo detecta como igual.
        var excepcion = await Assert.ThrowsAsync<BeneficiarioDuplicadoException>(
            () => _handler.HandleAsync(ComandoValido(" 12345678 "), TestContext.Current.CancellationToken));

        Assert.Single(_repository.Guardados);
        Assert.Equal("Ya existe un beneficiario registrado con el documento Dni 12345678.", excepcion.Message);
    }

    [Fact]
    public async Task Un_documento_invalido_no_llega_al_repositorio()
    {
        await Assert.ThrowsAsync<DocumentoIdentidadInvalidoException>(
            () => _handler.HandleAsync(ComandoValido("123"), TestContext.Current.CancellationToken));

        Assert.Empty(_repository.Guardados);
    }

    [Fact]
    public async Task Un_nombre_invalido_no_llega_al_repositorio()
    {
        var comando = ComandoValido() with { Nombres = "Ana123" };

        await Assert.ThrowsAsync<BeneficiarioInvalidoException>(
            () => _handler.HandleAsync(comando, TestContext.Current.CancellationToken));

        Assert.Empty(_repository.Guardados);
    }

    [Fact]
    public async Task El_comando_es_obligatorio()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, TestContext.Current.CancellationToken));
    }
}
