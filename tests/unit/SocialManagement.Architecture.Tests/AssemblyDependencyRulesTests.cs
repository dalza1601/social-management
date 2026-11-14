using System.Reflection;

namespace SocialManagement.Architecture.Tests;

/// <summary>
/// Reglas verificadas sobre los ensamblados compilados: detectan dependencias reales
/// (tipos usados en el código), incluso si llegaran de forma transitiva.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AssemblyDependencyRulesTests
{
    private static readonly string[] InfrastructureFrameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Data.SqlClient",
        "RabbitMQ.Client",
        "AWSSDK",
        "Yarp",
    ];

    public static TheoryData<string> Services => new() { "Beneficiarios", "Solicitudes" };

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_solo_usa_la_biblioteca_base_de_NET(string service)
    {
        var references = ReferencedAssemblies(service, "Domain");

        Assert.All(references, reference =>
            Assert.True(
                reference == "netstandard" || reference.StartsWith("System", StringComparison.Ordinal),
                $"Domain no debe depender de '{reference}'."));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_no_depende_de_infraestructura_ni_frameworks(string service)
    {
        var references = ReferencedAssemblies(service, "Application");

        Assert.DoesNotContain($"SocialManagement.{service}.Infrastructure", references);
        Assert.DoesNotContain($"SocialManagement.{service}.Api", references);
        AssertNoFrameworkDependencies(references);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Infrastructure_no_depende_de_la_Api(string service)
    {
        var references = ReferencedAssemblies(service, "Infrastructure");

        Assert.DoesNotContain($"SocialManagement.{service}.Api", references);
    }

    [Theory]
    [InlineData("Beneficiarios", "Solicitudes")]
    [InlineData("Solicitudes", "Beneficiarios")]
    public void Los_ensamblados_de_un_servicio_no_usan_los_de_otro(string service, string otherService)
    {
        foreach (var layer in new[] { "Domain", "Application", "Infrastructure" })
        {
            var references = ReferencedAssemblies(service, layer);
            Assert.DoesNotContain(references, r => r.StartsWith($"SocialManagement.{otherService}.", StringComparison.Ordinal));
        }
    }

    private static void AssertNoFrameworkDependencies(IReadOnlyList<string> references)
    {
        foreach (var framework in InfrastructureFrameworks)
        {
            Assert.DoesNotContain(references, r => r.StartsWith(framework, StringComparison.Ordinal));
        }
    }

    private static IReadOnlyList<string> ReferencedAssemblies(string service, string layer) =>
        Assembly.Load(new AssemblyName($"SocialManagement.{service}.{layer}"))
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();
}
