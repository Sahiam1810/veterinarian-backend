using Application.Common.Results;

namespace Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // Acceso denegado con code estable (p. ej. Authentication.PlatformAccessDenied).
    // Message = Description genérica del Error; no filtrar datos sensibles al front vía Message.
    public ForbiddenException(Error error) : base(error.Description)
    {
        Code = error.Code;
    }

    public string? Code { get; }
}
