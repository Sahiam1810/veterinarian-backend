namespace Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }

    // 401 con code estable (p. ej. AppointmentAction.PhoneMismatch).
    public UnauthorizedException(string message, string code) : base(message)
    {
        Code = code;
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public string? Code { get; }
}
