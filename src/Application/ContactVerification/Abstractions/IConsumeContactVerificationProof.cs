using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.Abstractions;

// Consume el proof de un solo uso (3.3/3.4). No es ruta HTTP pública en el kickoff.
public sealed record ConsumeContactVerificationProof(string Proof);

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
