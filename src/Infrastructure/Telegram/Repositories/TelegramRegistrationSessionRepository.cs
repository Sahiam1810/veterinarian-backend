using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramRegistrationSessionRepository(VeterinaryDbContext context)
    : ITelegramRegistrationSessionRepository
{
    public Task<TelegramRegistrationSession?> GetActiveByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken) =>
        context.Set<TelegramRegistrationSession>()
            .Where(session =>
                session.TelegramUserId == telegramUserId &&
                (session.Status == TelegramRegistrationSessionStatus.AwaitingEmail ||
                 session.Status == TelegramRegistrationSessionStatus.AwaitingOtp ||
                 session.Status == TelegramRegistrationSessionStatus.AwaitingProfile))
            .OrderByDescending(session => session.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TelegramRegistrationSession?> GetByCompletionTokenHashAsync(
        string completionTokenHash,
        CancellationToken cancellationToken) =>
        context.Set<TelegramRegistrationSession>()
            .FirstOrDefaultAsync(
                session =>
                    session.CompletionTokenHash == completionTokenHash &&
                    session.Status == TelegramRegistrationSessionStatus.AwaitingProfile,
                cancellationToken);

    public async Task AddAsync(
        TelegramRegistrationSession session,
        CancellationToken cancellationToken) =>
        await context.Set<TelegramRegistrationSession>().AddAsync(session, cancellationToken);

    public Task UpdateAsync(
        TelegramRegistrationSession session,
        CancellationToken cancellationToken)
    {
        context.Set<TelegramRegistrationSession>().Update(session);
        return Task.CompletedTask;
    }
}
