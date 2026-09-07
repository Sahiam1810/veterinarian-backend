using Application.ContactVerification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ContactVerification.Repositories;

public sealed class ContactVerificationSessionRepository(VeterinaryDbContext context)
    : IContactVerificationSessionRepository
{
    public Task<ContactVerificationSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        context.Set<ContactVerificationSession>()
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken);

    public Task<ContactVerificationSession?> GetActiveByPurposeAndDestinationAsync(
        ContactVerificationPurpose purpose,
        string destinationHash,
        CancellationToken cancellationToken) =>
        context.Set<ContactVerificationSession>()
            .FirstOrDefaultAsync(
                session => session.Purpose == purpose
                    && session.DestinationHash == destinationHash
                    && (session.Status == ContactVerificationSessionStatus.AwaitingOtp
                        || session.Status == ContactVerificationSessionStatus.ProofIssued),
                cancellationToken);

    public Task<ContactVerificationSession?> GetByProofHashAsync(
        string proofHash,
        CancellationToken cancellationToken) =>
        context.Set<ContactVerificationSession>()
            .FirstOrDefaultAsync(session => session.ProofHash == proofHash, cancellationToken);

    public async Task AddAsync(
        ContactVerificationSession session,
        CancellationToken cancellationToken) =>
        await context.Set<ContactVerificationSession>().AddAsync(session, cancellationToken);

    public Task UpdateAsync(
        ContactVerificationSession session,
        CancellationToken cancellationToken)
    {
        context.Set<ContactVerificationSession>().Update(session);
        return Task.CompletedTask;
    }
}
