using Application.Common.Results;

namespace Application.Common.Exceptions;

// 404 con code opcional para problem+json estable.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, string code) : base(message)
    {
        Code = code;
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public string? Code { get; }
}
