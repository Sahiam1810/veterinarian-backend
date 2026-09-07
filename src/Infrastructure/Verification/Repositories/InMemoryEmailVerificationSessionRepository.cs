using System.Collections.Concurrent;
using Application.Verification.Abstractions;
using Domain.Verification.Entities;
using Domain.Verification.Enums;

namespace Infrastructure.Verification.Repositories;

public sealed class InMemoryEmailVerificationSessionRepository : IEmailVerificationSessionRepository
{
    private readonly ConcurrentDictionary<Guid, EmailVerificationSession> _sessions = new();

    public Task<EmailVerificationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<EmailVerificationSession?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var active = _sessions.Values
            .FirstOrDefault(s => s.Email == normalized && s.Status == VerificationSessionStatus.AwaitingOtp);
        return Task.FromResult(active);
    }

    public Task AddAsync(EmailVerificationSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmailVerificationSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
