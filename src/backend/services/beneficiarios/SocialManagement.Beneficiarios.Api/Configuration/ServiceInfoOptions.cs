namespace SocialManagement.Beneficiarios.Api.Configuration;

/// <summary>
/// Datos descriptivos del servicio (sección <c>ServiceInfo</c>), expuestos en /version y en los logs.
/// </summary>
public sealed class ServiceInfoOptions
{
    public const string SectionName = "ServiceInfo";

    public string Name { get; set; } = "beneficiarios-api";

    public string Description { get; set; } = string.Empty;
}
