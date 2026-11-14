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
/// - Id distinto de Guid.Empty.
/// - Nombres y apellidos obligatorios, de máximo 100 caracteres, solo con letras, espacios,
///   apóstrofos, guiones y puntos.
/// - Documento de identidad válido (garantizado por el Value Object).
/// - Fecha de registro informada y en UTC.
/// El constructor es privado: la única forma de crear un beneficiario es <see cref="Registrar"/>.
/// </remarks>
public sealed class Beneficiario
{
    public const int LongitudMaximaNombre = 100;

    private Beneficiario(Guid id, string nombres, string apellidos, DocumentoIdentidad documento, DateTimeOffset fechaRegistroUtc)
    {
        Id = id;
        Nombres = nombres;
        Apellidos = apellidos;
        Documento = documento;
        FechaRegistroUtc = fechaRegistroUtc;
        Estado = EstadoBeneficiario.Activo;
    }

    public Guid Id { get; }

    public string Nombres { get; }

    public string Apellidos { get; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public DocumentoIdentidad Documento { get; }

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

        return new Beneficiario(
            // Guid versión 7: ordenable por fecha (mejor rendimiento en índices de base de datos).
            Guid.CreateVersion7(fechaRegistroUtc),
            NormalizarNombre(nombres, "nombres"),
            NormalizarNombre(apellidos, "apellidos"),
            documento,
            fechaRegistroUtc);
    }

    /// <summary>Marca al beneficiario como inactivo. Si ya lo está, no cambia nada.</summary>
    public void Desactivar() => Estado = EstadoBeneficiario.Inactivo;

    /// <summary>Marca al beneficiario como activo. Si ya lo está, no cambia nada.</summary>
    public void Activar() => Estado = EstadoBeneficiario.Activo;

    /// <summary>Elimina espacios sobrantes y valida longitud y caracteres permitidos.</summary>
    private static string NormalizarNombre(string? valor, string campo)
    {
        var normalizado = string.Join(' ', (valor ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (normalizado.Length == 0)
        {
            throw new BeneficiarioInvalidoException($"Los {campo} son obligatorios.");
        }

        if (normalizado.Length > LongitudMaximaNombre)
        {
            throw new BeneficiarioInvalidoException($"Los {campo} no pueden superar {LongitudMaximaNombre} caracteres.");
        }

        if (!normalizado.All(c => char.IsLetter(c) || c is ' ' or '\'' or '-' or '.'))
        {
            throw new BeneficiarioInvalidoException($"Los {campo} solo pueden contener letras, espacios, apóstrofos, guiones y puntos.");
        }

        return normalizado;
    }
}
