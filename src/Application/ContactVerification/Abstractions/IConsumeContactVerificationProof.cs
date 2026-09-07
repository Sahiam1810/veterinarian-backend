using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.Abstractions;

// Consume el proof de un solo uso (3.3/3.4). Exige sessionId + proof; no autoriza solo con sessionId.
public sealed record ConsumeContactVerificationProof(Guid SessionId, string Proof);

public sealed record ConsumedContactVerificationProof(
    Guid SessionId,
    ContactVerificationPurpose Purpose,
    Guid? SubjectUserId);

public interface IConsumeContactVerificationProof
{
    Task<ConsumedContactVerificationProof> ConsumeAsync(
        ConsumeContactVerificationProof request,
        CancellationToken cancellationToken);
}
