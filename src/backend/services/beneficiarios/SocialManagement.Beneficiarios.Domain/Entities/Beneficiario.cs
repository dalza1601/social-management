using SocialManagement.Beneficiarios.Domain.Enums;
using SocialManagement.Beneficiarios.Domain.Exceptions;
using SocialManagement.Beneficiarios.Domain.ValueObjects;

namespace SocialManagement.Beneficiarios.Domain.Entities;

/// <summary>
/// Entidad: persona registrada como beneficiaria de los programas sociales.
/// Se identifica por su <see cref="Id"/>; sus datos pueden cambiar sin dejar de ser la misma persona.
/// </summary>
/// <remarks>
/// Invariantes (siempre se cumplen para cualquier instancia):
/// - Nombres y apellidos obligatorios, de máximo 100 caracteres, solo con letras, espacios,
///   apóstrofos, guiones y puntos.
/// - Documento de identidad válido (garantizado por el Value Object).
/// - Fecha de registro informada y en UTC.
/// El constructor es privado: la única forma de crear un beneficiario es <see cref="Registrar"/>.
/// Identidad: <see cref="Id"/> es un entero que asigna la base de datos al guardar (columna IDENTITY);
/// vale 0 mientras el beneficiario no se haya guardado.
/// El dominio no sabe cómo se guarda: la clave primaria, las columnas y los índices se configuran en
/// Infrastructure (Persistence/Configurations/BeneficiarioConfiguration.cs).
/// </remarks>
public sealed class Beneficiario : AuditableEntity
{
    public const int LongitudMaximaNombre = 100;

    /// <summary>
    /// Solo para EF Core: materializa beneficiarios leídos de la base de datos (asigna las propiedades después).
    /// Es privado, así que el código de la aplicación no puede crear un beneficiario sin pasar por <see cref="Registrar"/>.
    /// </summary>
    private Beneficiario()
    {
        Nombres = null!;
        Apellidos = null!;
        Documento = null!;
    }

    private Beneficiario(string nombres, string apellidos, DocumentoIdentidad documento, DateTimeOffset fechaRegistroUtc)
    {
        Nombres = nombres;
        Apellidos = apellidos;
        Documento = documento;
        FechaRegistroUtc = fechaRegistroUtc;
        Estado = EstadoBeneficiario.Activo;
    }

    /// <summary>
    /// Identidad del beneficiario: entero autoincremental asignado por la base de datos al guardar.
    /// 0 = todavía no guardado. El setter es privado: solo EF Core lo escribe (después del INSERT).
    /// </summary>
    public int Id { get; private set; }

    public string Nombres { get; private set; }

    public string Apellidos { get; private set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public DocumentoIdentidad Documento { get; private set; }

    public DateTimeOffset FechaRegistroUtc { get; }

    public EstadoBeneficiario Estado { get; private set; }

    public bool EstaActivo => Estado == EstadoBeneficiario.Activo;

    /// <summary>Registra un nuevo beneficiario. Queda en estado <see cref="EstadoBeneficiario.Activo"/>.</summary>
    /// <param name="fechaRegistro">Momento del registro. La aporta quien llama (por ejemplo, un TimeProvider).</param>
    /// <exception cref="BeneficiarioInvalidoException">Si algún dato incumple una invariante.</exception>
    public static Beneficiario Registrar(
        string? nombres,
        string? apellidos,
        DocumentoIdentidad? documento,
        DateTimeOffset fechaRegistro)
    {
        if (documento is null)
        {
            throw new BeneficiarioInvalidoException("El documento de identidad es obligatorio.");
        }

        if (fechaRegistro == default)
        {
            throw new BeneficiarioInvalidoException("La fecha de registro es obligatoria.");
        }

        var fechaRegistroUtc = fechaRegistro.ToUniversalTime();

        // Sin Id: lo asigna la base de datos al guardar.
        return new Beneficiario(
            ValidarNombre(nombres, "nombres"),
            ValidarNombre(apellidos, "apellidos"),
            documento,
            fechaRegistroUtc);
    }

    /// <summary>
    /// Corrige los datos personales del beneficiario. Aplica las mismas invariantes que <see cref="Registrar"/>.
    /// La identidad (<see cref="Id"/>), la fecha de registro y el estado no cambian.
    /// </summary>
    /// <exception cref="BeneficiarioInvalidoException">Si algún dato incumple una invariante.</exception>
    public void ActualizarDatos(string? nombres, string? apellidos, DocumentoIdentidad? documento)
    {
        if (documento is null)
        {
            throw new BeneficiarioInvalidoException("El documento de identidad es obligatorio.");
        }

        // Se validan todos los datos ANTES de modificar: o cambia todo, o no cambia nada.
        var nombresValidos = ValidarNombre(nombres, "nombres");
        var apellidosValidos = ValidarNombre(apellidos, "apellidos");

        Nombres = nombresValidos;
        Apellidos = apellidosValidos;
        Documento = documento;
    }

    /// <summary>Marca al beneficiario como inactivo. Si ya lo está, no cambia nada.</summary>
    public void Desactivar() => Estado = EstadoBeneficiario.Inactivo;

    /// <summary>Marca al beneficiario como activo. Si ya lo está, no cambia nada.</summary>
    public void Activar() => Estado = EstadoBeneficiario.Activo;

    /// <summary>Elimina los espacios sobrantes: "  Ana   María " → "Ana María".</summary>
    public static string NormalizarNombre(string? valor) =>
        string.Join(' ', (valor ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>¿Solo contiene letras (con tildes y eñes), espacios, apóstrofos, guiones y puntos?</summary>
    /// <remarks>Regla única que usan <see cref="Registrar"/> y los validadores de entrada (FluentValidation).</remarks>
    public static bool TieneCaracteresPermitidos(string nombre) =>
        nombre.All(c => char.IsLetter(c) || c is ' ' or '\'' or '-' or '.');

    /// <summary>Normaliza y valida longitud y caracteres permitidos; lanza excepción si no cumple.</summary>
    private static string ValidarNombre(string? valor, string campo)
    {
        var normalizado = NormalizarNombre(valor);

        if (normalizado.Length == 0)
        {
            throw new BeneficiarioInvalidoException($"Los {campo} son obligatorios.");
        }

        if (normalizado.Length > LongitudMaximaNombre)
        {
            throw new BeneficiarioInvalidoException($"Los {campo} no pueden superar {LongitudMaximaNombre} caracteres.");
        }

        if (!TieneCaracteresPermitidos(normalizado))
        {
            throw new BeneficiarioInvalidoException($"Los {campo} solo pueden contener letras, espacios, apóstrofos, guiones y puntos.");
        }

        return normalizado;
    }
}
