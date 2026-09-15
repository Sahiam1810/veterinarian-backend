using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.ContactVerification.Security;
using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.UseCases;

// Consume proof de un solo uso de forma atómica (3.2 puerto; callers 3.3/3.4).
public sealed class ConsumeContactVerificationProofHandler(
    IContactVerificationSessionRepository sessions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IConsumeContactVerificationProof
{
    public async Task<ConsumedContactVerificationProof> ConsumeAsync(
        ConsumeContactVerificationProof request,
        CancellationToken cancellationToken)
    {
        if (request.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Proof))
        {
            throw new ContactVerificationException(ContactVerificationErrors.ProofInvalid);
        }

        var session = await sessions.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new ContactVerificationException(ContactVerificationErrors.SessionNotFound);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (session.Status == ContactVerificationSessionStatus.Consumed)
        {
            throw new ContactVerificationException(ContactVerificationErrors.ProofAlreadyConsumed);
        }

        if (session.Status == ContactVerificationSessionStatus.Expired
            || (session.Status == ContactVerificationSessionStatus.ProofIssued
                && (session.ProofExpiresAt is null || now >= session.ProofExpiresAt)))
        {
            if (session.Status == ContactVerificationSessionStatus.ProofIssued)
            {
                session.Expire(now);
                await sessions.UpdateAsync(session, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            throw new ContactVerificationException(ContactVerificationErrors.ProofExpired);
        }

        if (session.Status != ContactVerificationSessionStatus.ProofIssued
            || session.ProofHash is null
            || !ContactVerificationProof.Matches(request.Proof, session.ProofHash))
        {
            throw new ContactVerificationException(ContactVerificationErrors.ProofInvalid);
        }

        var proofHash = session.ProofHash;
        var purpose = session.Purpose;
        var subjectUserId = session.SubjectUserId;
        var destinationHash = session.DestinationHash;

        var consumed = await sessions.TryConsumeProofAsync(
            session.Id,
            proofHash,
            now,
            cancellationToken);

        if (!consumed)
        {
            var latest = await sessions.GetByIdAsync(session.Id, cancellationToken);
            if (latest?.Status == ContactVerificationSessionStatus.Consumed)
            {
                throw new ContactVerificationException(ContactVerificationErrors.ProofAlreadyConsumed);
            }

            if (latest?.Status == ContactVerificationSessionStatus.Expired
                || (latest?.ProofExpiresAt is not null && now >= latest.ProofExpiresAt))
            {
                throw new ContactVerificationException(ContactVerificationErrors.ProofExpired);
            }

            throw new ContactVerificationException(ContactVerificationErrors.ProofInvalid);
        }

        return new ConsumedContactVerificationProof(
            session.Id,
            purpose,
            subjectUserId,
            destinationHash);
    }
}
