namespace SocialManagement.Beneficiarios.Api.IntegrationTests;

/// <summary>Fábrica con una configuración incompleta, para probar los mensajes de error de arranque.</summary>
public sealed class MisconfiguredBeneficiariosApiFactory(Dictionary<string, string?> overrides)
    : BeneficiariosApiFactory(overrides);
