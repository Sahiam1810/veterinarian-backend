using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;

namespace Infrastructure.ContactVerification.Stubs;

// Kickoff: 3.2 reemplaza este stub (emitir proof de un solo uso).
public sealed class ContactEmailVerificationConfirmStub : IConfirmContactEmailVerification
{
    public Task<ConfirmContactEmailVerificationResult> ConfirmAsync(
        ConfirmContactEmailVerification request,
        CancellationToken cancellationToken) =>
        throw new ContactVerificationNotImplementedException(ContactVerificationErrors.NotImplemented);
}
