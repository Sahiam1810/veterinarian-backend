using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;

namespace Infrastructure.ContactVerification.Stubs;

// Kickoff: 3.1 reemplaza este stub por el envío real (hash, no OTP en claro).
public sealed class ContactEmailVerificationRequestStub : IRequestContactEmailVerification
{
    public Task<RequestContactEmailVerificationResult> RequestAsync(
        RequestContactEmailVerification request,
        CancellationToken cancellationToken) =>
        throw new ContactVerificationNotImplementedException(ContactVerificationErrors.NotImplemented);
}
