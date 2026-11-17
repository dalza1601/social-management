using System.Security.Cryptography;
using System.Text;

namespace SocialManagement.Beneficiarios.Api.Security;

/// <summary>Comparación de secretos (claves, contraseñas) que no revela información por el tiempo de respuesta.</summary>
internal static class SecretComparer
{
    /// <summary>
    /// Compara resúmenes SHA-256 en tiempo constante: el tiempo no depende de cuántos caracteres coinciden
    /// ni de la longitud de los valores.
    /// </summary>
    public static bool FixedTimeEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
