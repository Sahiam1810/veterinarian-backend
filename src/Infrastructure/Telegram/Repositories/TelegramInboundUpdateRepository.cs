using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramInboundUpdateRepository(VeterinaryDbContext context)
    : ITelegramInboundUpdateRepository
{
    public Task<bool> ExistsAsync(long updateId, CancellationToken cancellationToken) =>
        context.Set<TelegramInboundUpdate>().AnyAsync(update => update.Id == updateId, cancellationToken);

    public Task<TelegramInboundUpdate?> GetByIdAsync(long updateId, CancellationToken cancellationToken) =>
        context.Set<TelegramInboundUpdate>().FirstOrDefaultAsync(update => update.Id == updateId, cancellationToken);

    public async Task<TelegramInboundUpdate?> ClaimNextAsync(DateTime now, CancellationToken cancellationToken)
    {
        var candidateId = await context.Set<TelegramInboundUpdate>()
            .AsNoTracking()
            .Where(update => update.Status == TelegramInboundUpdateStatus.Pending && update.NextAttemptAt <= now)
            .OrderBy(update => update.NextAttemptAt)
            .ThenBy(update => update.Id)
            .Select(update => (long?)update.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidateId is null)
        {
            return null;
        }

        var affected = await context.Set<TelegramInboundUpdate>()
            .Where(update => update.Id == candidateId && update.Status == TelegramInboundUpdateStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(update => update.Status, TelegramInboundUpdateStatus.Processing)
                .SetProperty(update => update.Attempts, update => update.Attempts + 1)
                .SetProperty(update => update.UpdatedAt, now), cancellationToken);
        return affected == 1
            ? await context.Set<TelegramInboundUpdate>().FirstAsync(update => update.Id == candidateId, cancellationToken)
            : null;
    }

    public async Task AddAsync(TelegramInboundUpdate update, CancellationToken cancellationToken) =>
        await context.Set<TelegramInboundUpdate>().AddAsync(update, cancellationToken);

    public Task UpdateAsync(TelegramInboundUpdate update, CancellationToken cancellationToken)
    {
        context.Set<TelegramInboundUpdate>().Update(update);
        return Task.CompletedTask;
    }
}
