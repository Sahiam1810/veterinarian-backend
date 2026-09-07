using Domain.Verification.Entities;

namespace Application.Verification.Abstractions;

public interface IEmailVerificationSessionRepository
{
    Task<EmailVerificationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EmailVerificationSession?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(EmailVerificationSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(EmailVerificationSession session, CancellationToken cancellationToken = default);
}
