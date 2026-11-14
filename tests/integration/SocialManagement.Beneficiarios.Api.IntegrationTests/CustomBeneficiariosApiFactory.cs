namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>Fábrica con configuración y ambiente a elección (p. ej. Production con credenciales).</summary>
public sealed class CustomBeneficiariosApiFactory(Dictionary<string, string?> overrides, string environment)
    : BeneficiariosApiFactory(overrides, environment);
