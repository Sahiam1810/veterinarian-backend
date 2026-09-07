using Application.Common.Results;

namespace Application.ContactVerification.Errors;

// Fallo tipado de verificación de contacto (code estable; Description genérica).
public sealed class ContactVerificationException : Exception
{
    public ContactVerificationException(Error error)
        : base(error.Description)
    {
        Code = error.Code;
    }

    public string Code { get; }
}
