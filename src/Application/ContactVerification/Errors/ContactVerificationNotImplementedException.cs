using Application.Common.Results;

namespace Application.ContactVerification.Errors;

// Reserva el contrato HTTP/puerto hasta que 3.1–3.4 lo implementen.
public sealed class ContactVerificationNotImplementedException : Exception
{
    public ContactVerificationNotImplementedException(Error error)
        : base(error.Description)
    {
        Code = error.Code;
    }

    public string Code { get; }
}
