using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramIdentitySessionRepository(VeterinaryDbContext context)
    : ITelegramIdentitySessionRepository
{
    public Task<TelegramIdentitySession?> GetCurrentByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken) =>
        context.Set<TelegramIdentitySession>()
            .Where(session =>
                session.TelegramUserId == telegramUserId &&
                session.Status != TelegramIdentitySessionStatus.Cancelled &&
                session.Status != TelegramIdentitySessionStatus.Expired &&
                session.Status != TelegramIdentitySessionStatus.Blocked)
            .OrderByDescending(session => session.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TelegramIdentitySession?> GetByPendingInboundUpdateIdAsync(
        long pendingInboundUpdateId,
        CancellationToken cancellationToken) =>
        context.Set<TelegramIdentitySession>()
            .FirstOrDefaultAsync(
                session => session.PendingInboundUpdateId == pendingInboundUpdateId,
                cancellationToken);

    public async Task AddAsync(
        TelegramIdentitySession session,
        CancellationToken cancellationToken) =>
        await context.Set<TelegramIdentitySession>().AddAsync(session, cancellationToken);

    public Task UpdateAsync(
        TelegramIdentitySession session,
        CancellationToken cancellationToken)
    {
        context.Set<TelegramIdentitySession>().Update(session);
        return Task.CompletedTask;
    }
}
