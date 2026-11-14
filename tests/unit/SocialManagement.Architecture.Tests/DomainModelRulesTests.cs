using System.Reflection;

namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Reglas de diseño del modelo de dominio (sesión 02), verificadas en todos los microservicios:
/// el estado solo cambia mediante métodos del dominio y los objetos nacen válidos.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DomainModelRulesTests
{
    public static TheoryData<string> Services => new() { "Beneficiarios", "Solicitudes" };

    [Theory]
    [MemberData(nameof(Services))]
    public void Entidades_y_value_objects_no_exponen_setters_publicos(string service)
    {
        var types = DomainTypes(service, "Entities").Concat(DomainTypes(service, "ValueObjects"));

        foreach (var type in types)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.False(
                    property.SetMethod?.IsPublic ?? false,
                    $"{type.Name}.{property.Name} tiene un setter público: el estado debe cambiar mediante métodos del dominio.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Entidades_y_value_objects_no_tienen_constructores_publicos(string service)
    {
        var types = DomainTypes(service, "Entities").Concat(DomainTypes(service, "ValueObjects"));

        foreach (var type in types)
        {
            Assert.True(
                type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0,
                $"{type.Name} tiene un constructor público: debe crearse con un método de fábrica que valide sus invariantes.");
        }
    }

    /// <summary>
    /// El dominio no sabe cómo se guarda: nada de [Key], [Required], [MaxLength], [Table], [Column]...
    /// El mapeo va en Infrastructure/Persistence/Configurations (IEntityTypeConfiguration).
    /// </summary>
    [Theory]
    [MemberData(nameof(Services))]
    public void Entidades_y_value_objects_no_usan_atributos_de_persistencia(string service)
    {
        var types = DomainTypes(service, "Entities").Concat(DomainTypes(service, "ValueObjects"));

        foreach (var type in types)
        {
            var members = new MemberInfo[] { type }.Concat(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            foreach (var member in members)
            {
                var persistenceAttributes = member.GetCustomAttributes(inherit: false)
                    .Select(attribute => attribute.GetType())
                    .Where(attributeType => attributeType.Namespace?.StartsWith("System.ComponentModel.DataAnnotations", StringComparison.Ordinal) == true
                                            || attributeType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
                    .Select(attributeType => attributeType.Name)
                    .ToList();

                Assert.True(
                    persistenceAttributes.Count == 0,
                    $"{type.Name}.{member.Name} usa {string.Join(", ", persistenceAttributes)}: configura la persistencia en Infrastructure/Persistence/Configurations.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Las_excepciones_del_dominio_heredan_de_DomainException(string service)
    {
        var assembly = DomainAssembly(service);
        var baseType = assembly.GetType($"SocialManagement.{service}.Domain.Exceptions.DomainException", throwOnError: true)!;

        foreach (var type in DomainTypes(service, "Exceptions").Where(t => t != baseType))
        {
            Assert.True(baseType.IsAssignableFrom(type), $"{type.Name} debe heredar de DomainException.");
        }
    }

    [Fact]
    public void Las_reglas_inspeccionan_el_modelo_real_de_Beneficiarios()
    {
        // Evita que las pruebas anteriores pasen "en vacío" si cambia un namespace.
        Assert.Contains(DomainTypes("Beneficiarios", "Entities"), t => t.Name == "Beneficiario");
        Assert.Contains(DomainTypes("Beneficiarios", "ValueObjects"), t => t.Name == "DocumentoIdentidad");
    }

    private static Assembly DomainAssembly(string service) =>
        Assembly.Load(new AssemblyName($"SocialManagement.{service}.Domain"));

    private static IEnumerable<Type> DomainTypes(string service, string folder) =>
        DomainAssembly(service).GetTypes()
            .Where(t => t.Namespace == $"SocialManagement.{service}.Domain.{folder}"
                        && t is { IsClass: true, IsNested: false }
                        && !t.Name.StartsWith('<'));
}
