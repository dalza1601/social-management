using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;
using SocialManagement.Beneficiarios.Application.Validators;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class ActualizarBeneficiarioHandlerTests
{
    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ActualizarBeneficiarioHandler _handler;

    public ActualizarBeneficiarioHandlerTests() =>
        _handler = new ActualizarBeneficiarioHandler(new ActualizarBeneficiarioCommandValidator(), _repository, _unitOfWork);

    [Fact]
    public async Task Actualiza_los_datos_y_confirma_los_cambios()
    {
        var ana = await _repository.AgregarAsync("Ana", "Quispe", "12345678");

        var resultado = await _handler.HandleAsync(
            new ActualizarBeneficiarioCommand(ana.Id, "  Ana   María ", "Quispe Rojas", TipoDocumentoIdentidad.Dni, "12345678"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(resultado);
        Assert.Equal("Ana María Quispe Rojas", resultado.NombreCompleto);   // nombres normalizados por el dominio
        Assert.Equal("Ana María", ana.Nombres);                             // la entidad rastreada cambió
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Puede_cambiar_el_documento_por_uno_libre()
    {
        var ana = await _repository.AgregarAsync("Ana", "Quispe", "12345678");

        var resultado = await _handler.HandleAsync(
            new ActualizarBeneficiarioCommand(ana.Id, "Ana", "Quispe", TipoDocumentoIdentidad.Pasaporte, "AB123456"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(resultado);
        Assert.Equal("Pasaporte", resultado.TipoDocumento);
        Assert.Equal("AB123456", resultado.NumeroDocumento);
    }

    [Fact]
    public async Task Rechaza_un_documento_que_pertenece_a_otro_beneficiario()
    {
        var ana = await _repository.AgregarAsync("Ana", "Quispe", "12345678");
        await _repository.AgregarAsync("Luis", "Huamán", "87654321");

        await Assert.ThrowsAsync<BeneficiarioDuplicadoException>(() => _handler.HandleAsync(
            new ActualizarBeneficiarioCommand(ana.Id, "Ana", "Quispe", TipoDocumentoIdentidad.Dni, "87654321"),
            TestContext.Current.CancellationToken));

        Assert.Equal("12345678", ana.Documento.Numero);   // sin cambios
        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Devuelve_null_si_el_beneficiario_no_existe()
    {
        var resultado = await _handler.HandleAsync(
            new ActualizarBeneficiarioCommand(999, "Ana", "Quispe", TipoDocumentoIdentidad.Dni, "12345678"),
            TestContext.Current.CancellationToken);

        Assert.Null(resultado);
        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Valida_la_entrada_antes_de_consultar_el_repositorio()
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(
            new ActualizarBeneficiarioCommand(0, "Ana123", "", TipoDocumentoIdentidad.Dni, "123"),
            TestContext.Current.CancellationToken));

        Assert.Equal(
            ["Apellidos", "Id", "Nombres", "NumeroDocumento"],
            excepcion.Errors.Select(e => e.PropertyName).Order());
        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }
}
