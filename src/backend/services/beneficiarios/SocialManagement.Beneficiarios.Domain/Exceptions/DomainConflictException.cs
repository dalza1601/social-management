namespace SocialManagement.Beneficiarios.Domain.Exceptions;

/// <summary>
/// Clase base de las reglas de dominio que fallan por CONFLICTO con datos que ya existen
/// (p. ej. un documento repetido). La API las traduce a HTTP 409 Conflict en lugar de 422.
/// </summary>
public abstract class DomainConflictException : DomainException
{
    protected DomainConflictException(string message)
        : base(message)
    {
    }
}
