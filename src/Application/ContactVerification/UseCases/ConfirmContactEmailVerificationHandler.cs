using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.ContactVerification.Security;
using Application.Verification.Abstractions;
using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.UseCases;

// Confirma OTP de correo y emite proof de un solo uso (3.2).
public sealed class ConfirmContactEmailVerificationHandler(
    IContactVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IContactVerificationSettings settings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IConfirmContactEmailVerification
{
    public async Task<ConfirmContactEmailVerificationResult> ConfirmAsync(
        ConfirmContactEmailVerification request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ContactVerificationException(ContactVerificationErrors.InvalidCode);
        }

        var session = await sessions.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new ContactVerificationException(ContactVerificationErrors.SessionNotFound);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (session.Status == ContactVerificationSessionStatus.Blocked)
        {
            throw new ContactVerificationException(ContactVerificationErrors.Blocked);
        }

        if (session.Status == ContactVerificationSessionStatus.Expired
            || (session.Status == ContactVerificationSessionStatus.AwaitingOtp
                && (session.ExpiresAt is null || now >= session.ExpiresAt)))
        {
            if (session.Status == ContactVerificationSessionStatus.AwaitingOtp)
            {
                session.Expire(now);
                await sessions.UpdateAsync(session, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            throw new ContactVerificationException(ContactVerificationErrors.Expired);
        }

        if (session.Status != ContactVerificationSessionStatus.AwaitingOtp)
        {
            // ProofIssued/Consumed/Cancelled: no confirma de nuevo (sin filtrar estado interno).
            throw new ContactVerificationException(ContactVerificationErrors.SessionNotFound);
        }

        if (session.OtpHash is null || !otpProtector.Verify(request.Code, session.OtpHash))
        {
            session.RegisterFailedAttempt(settings.OtpMaximumAttempts, now);
            await sessions.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (session.Status == ContactVerificationSessionStatus.Blocked)
            {
                throw new ContactVerificationException(ContactVerificationErrors.Blocked);
            }

            throw new ContactVerificationException(ContactVerificationErrors.InvalidCode);
        }

        var proof = ContactVerificationProof.Generate();
        var proofHash = ContactVerificationProof.Hash(proof);
        session.IssueProof(proofHash, now.Add(settings.ProofLifetime), now);
        await sessions.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmContactEmailVerificationResult(session.Id, proof);
    }
}
