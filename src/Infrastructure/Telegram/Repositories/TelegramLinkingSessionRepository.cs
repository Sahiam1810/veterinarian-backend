using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramLinkingSessionRepository(VeterinaryDbContext context)
    : ITelegramLinkingSessionRepository
{
    public Task<TelegramLinkingSession?> GetActiveByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken) =>
        context.Set<TelegramLinkingSession>()
            .Where(session =>
                session.TelegramUserId == telegramUserId &&
                (session.Status == TelegramLinkingSessionStatus.AwaitingEmail ||
                 session.Status == TelegramLinkingSessionStatus.AwaitingOtp))
            .OrderByDescending(session => session.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        TelegramLinkingSession session,
        CancellationToken cancellationToken) =>
        await context.Set<TelegramLinkingSession>().AddAsync(session, cancellationToken);

    public Task UpdateAsync(
        TelegramLinkingSession session,
        CancellationToken cancellationToken)
    {
        context.Set<TelegramLinkingSession>().Update(session);
        return Task.CompletedTask;
    }
}
