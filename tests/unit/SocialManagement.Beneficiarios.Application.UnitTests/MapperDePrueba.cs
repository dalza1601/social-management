using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using SocialManagement.Beneficiarios.Application;

namespace SocialManagement.Beneficiarios.Application.UnitTests;

/// <summary>
/// IMapper con los MISMOS perfiles que registra AddApplication (carpeta Mappings de Application),
/// para probar los casos de uso sin levantar el contenedor de DI.
/// </summary>
internal static class MapperDePrueba
{
    public static MapperConfiguration Configuracion() =>
        new(config => config.AddMaps(typeof(DependencyInjection).Assembly), NullLoggerFactory.Instance);

    public static IMapper Crear() => Configuracion().CreateMapper();
}
