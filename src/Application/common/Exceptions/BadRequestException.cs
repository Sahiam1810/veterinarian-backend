namespace Application.Common.Exceptions;

// 400 con code opcional para problem+json estable.
public sealed class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }

    public BadRequestException(string message, string code) : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
