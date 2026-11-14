namespace SocialManagement.Solicitudes.Api.Configuration;

/// <summary>
/// Proxies de confianza (sección <c>ForwardedHeaders</c>). Detrás del API Gateway, de un ingress o
/// de un balanceador, los encabezados X-Forwarded-Proto/Host/Prefix permiten que la API conozca la URL
/// pública real (HTTPS, dominio y prefijo /api/solicitudes). Se usa, por ejemplo, para el campo
/// "servers" del documento OpenAPI.
/// </summary>
/// <remarks>
/// Solo se aceptan esos encabezados si la petición llega desde un proxy de confianza: por defecto,
/// la propia máquina (loopback). En contenedores o Kubernetes agrega la red del Gateway en
/// <see cref="KnownNetworks"/> (p. ej. "172.16.0.0/12") o su IP en <see cref="KnownProxies"/>.
/// </remarks>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; } = true;

    /// <summary>Direcciones IP de proxies de confianza, p. ej. "10.0.0.5".</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Redes de confianza en notación CIDR, p. ej. "172.16.0.0/12".</summary>
    public string[] KnownNetworks { get; set; } = [];
}
