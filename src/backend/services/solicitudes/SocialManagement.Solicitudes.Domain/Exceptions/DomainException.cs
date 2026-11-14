namespace SocialManagement.Solicitudes.Domain.Exceptions;

/// <summary>
/// Clase base para las excepciones que representan una regla de dominio incumplida.
/// La API las traduce a una respuesta HTTP 422 (ProblemDetails) en lugar de un error 500.
/// </summary>
/// <remarks>
/// Las excepciones concretas (por ejemplo, de una entidad futura) heredarán de esta clase.
/// </remarks>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
