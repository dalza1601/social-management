using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.DesactivarBeneficiario;
using SocialManagement.Beneficiarios.Application.Validators;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class DesactivarBeneficiarioHandlerTests
{
    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DesactivarBeneficiarioHandler _handler;

    public DesactivarBeneficiarioHandlerTests() =>
        _handler = new DesactivarBeneficiarioHandler(new DesactivarBeneficiarioCommandValidator(), _repository, _unitOfWork);

    [Fact]
    public async Task Desactiva_al_beneficiario_sin_eliminarlo()
    {
        var ana = await _repository.AgregarAsync("Ana", "Quispe", "12345678");

        var resultado = await _handler.HandleAsync(new DesactivarBeneficiarioCommand(ana.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(resultado);
        Assert.Equal("Inactivo", resultado.Estado);
        Assert.Single(_repository.Guardados);   // baja lógica: el registro se conserva
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Desactivar_dos_veces_no_es_un_error()
    {
        var ana = await _repository.AgregarAsync("Ana", "Quispe", "12345678");

        await _handler.HandleAsync(new DesactivarBeneficiarioCommand(ana.Id), TestContext.Current.CancellationToken);
        var resultado = await _handler.HandleAsync(new DesactivarBeneficiarioCommand(ana.Id), TestContext.Current.CancellationToken);

        Assert.Equal("Inactivo", resultado?.Estado);
    }

    [Fact]
    public async Task Devuelve_null_si_el_beneficiario_no_existe()
    {
        var resultado = await _handler.HandleAsync(new DesactivarBeneficiarioCommand(999), TestContext.Current.CancellationToken);

        Assert.Null(resultado);
        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Rechaza_un_identificador_que_no_es_positivo(int id)
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.HandleAsync(new DesactivarBeneficiarioCommand(id), TestContext.Current.CancellationToken));

        Assert.Equal("Id", Assert.Single(excepcion.Errors).PropertyName);
    }
}
