using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.Abstractions;

public interface IContactVerificationSessionRepository
{
    Task<ContactVerificationSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    // Sesión viva (AwaitingOtp o ProofIssued) por propósito + hash de correo.
    Task<ContactVerificationSession?> GetActiveByPurposeAndDestinationAsync(
        ContactVerificationPurpose purpose,
        string destinationHash,
        CancellationToken cancellationToken);

    Task<ContactVerificationSession?> GetByProofHashAsync(
        string proofHash,
        CancellationToken cancellationToken);

    Task AddAsync(
        ContactVerificationSession session,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        ContactVerificationSession session,
        CancellationToken cancellationToken);
}
