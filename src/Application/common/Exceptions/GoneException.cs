using Application.Common.Results;

namespace Application.Common.Exceptions;

// Excepcion de recurso/ruta retirada (HTTP 410 Gone) con code estable opcional.
public class GoneException : Exception
{
    public GoneException(string message) : base(message)
    {
    }

    public GoneException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // Ruta retirada; Message = Description del Error; el front usa Code, no Message.
    public GoneException(Error error) : base(error.Description)
    {
        Code = error.Code;
    }

    public string? Code { get; }
}