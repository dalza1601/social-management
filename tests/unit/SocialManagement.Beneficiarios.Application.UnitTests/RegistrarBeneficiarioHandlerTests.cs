using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;
using SocialManagement.Beneficiarios.Application.Validators;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class RegistrarBeneficiarioHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly RegistrarBeneficiarioHandler _handler;

    public RegistrarBeneficiarioHandlerTests() =>
        _handler = new RegistrarBeneficiarioHandler(
            new RegistrarBeneficiarioCommandValidator(),
            _repository,
            _unitOfWork,
            new FakeTimeProvider(Ahora),
            MapperDePrueba.Crear());

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
    public async Task Devuelve_el_Id_entero_asignado_al_guardar()
    {
        var primero = await _handler.HandleAsync(ComandoValido("12345678"), TestContext.Current.CancellationToken);
        var segundo = await _handler.HandleAsync(ComandoValido("87654321"), TestContext.Current.CancellationToken);

        // El repositorio falso simula la columna IDENTITY de SQL Server: 1, 2, 3...
        Assert.Equal(1, primero.Id);
        Assert.Equal(2, segundo.Id);
    }

    [Fact]
    public async Task Confirma_los_cambios_una_sola_vez_con_la_unidad_de_trabajo()
    {
        await _handler.HandleAsync(ComandoValido(), TestContext.Current.CancellationToken);

        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
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
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);   // solo el primer registro se confirmó
        Assert.Equal("Ya existe un beneficiario registrado con el documento Dni 12345678.", excepcion.Message);
    }

    [Fact]
    public async Task Datos_invalidos_se_rechazan_en_la_validacion_antes_de_crear_el_dominio()
    {
        var comando = new RegistrarBeneficiarioCommand("Ana123", " ", TipoDocumentoIdentidad.Dni, "123");

        var excepcion = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.HandleAsync(comando, TestContext.Current.CancellationToken));

        // FluentValidation informa TODOS los campos con error a la vez.
        Assert.Equal(
            ["Apellidos", "Nombres", "NumeroDocumento"],
            excepcion.Errors.Select(e => e.PropertyName).Order());
        Assert.Empty(_repository.Guardados);
        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task El_comando_es_obligatorio()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, TestContext.Current.CancellationToken));
    }
}
