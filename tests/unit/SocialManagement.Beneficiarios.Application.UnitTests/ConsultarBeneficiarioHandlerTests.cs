using FluentValidation;
using SocialManagement.Beneficiarios.Application.Features.ConsultarBeneficiario;
using SocialManagement.Beneficiarios.Application.Validators;
using SocialManagement.Beneficiarios.Domain.Entities;
using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class ConsultarBeneficiarioHandlerTests
{
    private readonly FakeBeneficiarioRepository _repository = new();
    private readonly ConsultarBeneficiarioHandler _handler;

    public ConsultarBeneficiarioHandlerTests() =>
        _handler = new ConsultarBeneficiarioHandler(new ConsultarBeneficiarioQueryValidator(), _repository);

    [Fact]
    public async Task Rechaza_un_identificador_vacio()
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.HandleAsync(new ConsultarBeneficiarioQuery(Guid.Empty), TestContext.Current.CancellationToken));

        Assert.Equal("Id", Assert.Single(excepcion.Errors).PropertyName);
    }

    [Fact]
    public async Task Devuelve_los_datos_del_beneficiario_si_existe()
    {
        var beneficiario = Beneficiario.Registrar(
            "Luis", "Huamán Rojas",
            DocumentoIdentidad.Crear(TipoDocumentoIdentidad.CarnetExtranjeria, "001234567"),
            new DateTimeOffset(2026, 11, 13, 15, 30, 0, TimeSpan.Zero));
        await _repository.AgregarAsync(beneficiario, TestContext.Current.CancellationToken);

        var resultado = await _handler.HandleAsync(new ConsultarBeneficiarioQuery(beneficiario.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(resultado);
        Assert.Equal(beneficiario.Id, resultado.Id);
        Assert.Equal("Luis Huamán Rojas", resultado.NombreCompleto);
        Assert.Equal("CarnetExtranjeria", resultado.TipoDocumento);
        Assert.Equal("001234567", resultado.NumeroDocumento);
    }

    [Fact]
    public async Task Devuelve_null_si_no_existe()
    {
        var resultado = await _handler.HandleAsync(new ConsultarBeneficiarioQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Null(resultado);
    }
}
