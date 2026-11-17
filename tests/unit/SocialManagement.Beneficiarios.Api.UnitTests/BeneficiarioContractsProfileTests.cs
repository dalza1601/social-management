using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using SocialManagement.Beneficiarios.Api.Contracts.Beneficiarios;
using SocialManagement.Beneficiarios.Application.Common.Pagination;
using SocialManagement.Beneficiarios.Application.Features.ActualizarBeneficiario;
using SocialManagement.Beneficiarios.Application.Features.ListarBeneficiarios;
using SocialManagement.Beneficiarios.Application.Features.RegistrarBeneficiario;
using SocialManagement.Beneficiarios.Domain.Enums;
using ApiLayer = SocialManagement.Beneficiarios.Api.DependencyInjection;
using ApplicationLayer = SocialManagement.Beneficiarios.Application.DependencyInjection;

namespace SocialManagement.Beneficiarios.Api.UnitTests;

/// <summary>Perfiles de AutoMapper de la Api: contratos HTTP → comandos de Application.</summary>
[Trait("Category", "Unit")]
public sealed class BeneficiarioContractsProfileTests
{
    // Mismos perfiles que registra la aplicación: los de la Api y los de Application.
    private static readonly MapperConfiguration Configuracion = new(
        config => config.AddMaps(typeof(ApiLayer).Assembly, typeof(ApplicationLayer).Assembly),
        NullLoggerFactory.Instance);

    private readonly IMapper _mapper = Configuracion.CreateMapper();

    [Fact]
    public void La_configuracion_de_mapeo_de_toda_la_aplicacion_es_valida()
    {
        Configuracion.AssertConfigurationIsValid();
    }

    [Fact]
    public void Mapea_el_request_de_registro_al_comando()
    {
        var command = _mapper.Map<RegistrarBeneficiarioCommand>(
            new RegistrarBeneficiarioRequest("Ana María", "Quispe Mamani", TipoDocumentoIdentidad.Dni, "12345678"));

        Assert.Equal(new RegistrarBeneficiarioCommand("Ana María", "Quispe Mamani", TipoDocumentoIdentidad.Dni, "12345678"), command);
    }

    [Fact]
    public void Los_campos_ausentes_llegan_vacios_para_que_los_rechace_el_validador()
    {
        var command = _mapper.Map<RegistrarBeneficiarioCommand>(new RegistrarBeneficiarioRequest(null, null, null, null));

        Assert.Equal(string.Empty, command.Nombres);
        Assert.Equal(string.Empty, command.Apellidos);
        Assert.Equal(string.Empty, command.NumeroDocumento);
        Assert.False(Enum.IsDefined(command.TipoDocumento));   // 0: "El tipo de documento no es válido."
    }

    [Fact]
    public void La_query_plana_del_listado_se_agrupa_en_BeneficiarioQueryParameters()
    {
        var request = new ListarBeneficiariosRequest { PageNumber = 2, PageSize = 25, SearchTerm = "quispe", Sort = "-fechaRegistro" };

        var parameters = _mapper.Map<BeneficiarioQueryParameters>(request);

        Assert.Equal(new BeneficiarioQueryParameters(new PaginationRequest(2, 25), "quispe", "-fechaRegistro"), parameters);
        Assert.Equal("fecharegistro", parameters.SortField);
        Assert.True(parameters.SortDescending);
    }

    [Fact]
    public void Sin_parametros_el_listado_usa_la_primera_pagina_sin_busqueda_y_con_el_orden_por_defecto()
    {
        var parameters = _mapper.Map<BeneficiarioQueryParameters>(new ListarBeneficiariosRequest());

        Assert.Equal(new PaginationRequest(1, PaginationRequest.DefaultPageSize), parameters.Pagination);
        Assert.True(string.IsNullOrEmpty(parameters.SearchTerm));
        Assert.Null(parameters.SortField);
    }

    [Fact]
    public void El_Id_de_la_actualizacion_lo_asigna_el_controller_desde_la_ruta()
    {
        var request = new ActualizarBeneficiarioRequest("Luis", "Huamán", TipoDocumentoIdentidad.Pasaporte, "AB123456");

        var command = _mapper.Map<ActualizarBeneficiarioCommand>(request) with { Id = 25 };

        Assert.Equal(new ActualizarBeneficiarioCommand(25, "Luis", "Huamán", TipoDocumentoIdentidad.Pasaporte, "AB123456"), command);
    }
}
