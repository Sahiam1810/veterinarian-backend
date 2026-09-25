namespace Application.Common.Exceptions;

// Conflicto de negocio; Code opcional para problem+json estable hacia el front.
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, string code) : base(message)
    {
        Code = code;
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // Código estable de dominio p. ej. "Users.SomeConflict".
    public string? Code { get; }
}
