using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;

namespace Infrastructure.ContactVerification.Stubs;

// Kickoff: 3.3/3.4 reemplazan este stub al consumir el proof.
public sealed class ContactVerificationProofConsumerStub : IConsumeContactVerificationProof
{
    public Task<ConsumedContactVerificationProof> ConsumeAsync(
        ConsumeContactVerificationProof request,
        CancellationToken cancellationToken) =>
        throw new ContactVerificationNotImplementedException(ContactVerificationErrors.NotImplemented);
}
