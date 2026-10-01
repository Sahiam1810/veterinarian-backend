using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramInboundUpdateRepository(VeterinaryDbContext context)
    : ITelegramInboundUpdateRepository
{
    private const int MaxClaimAttempts = 5;

    public Task<bool> ExistsAsync(long updateId, CancellationToken cancellationToken) =>
        context.Set<TelegramInboundUpdate>().AnyAsync(update => update.Id == updateId, cancellationToken);

    public Task<TelegramInboundUpdate?> GetByIdAsync(long updateId, CancellationToken cancellationToken) =>
        context.Set<TelegramInboundUpdate>().FirstOrDefaultAsync(update => update.Id == updateId, cancellationToken);

    public async Task<TelegramInboundUpdate?> ClaimNextAsync(
        DateTime now,
        DateTime staleBefore,
        CancellationToken cancellationToken)
    {
        var sets = context.Set<TelegramInboundUpdate>();
        for (var attempt = 0; attempt < MaxClaimAttempts; attempt++)
        {
            var candidateId = await sets
                .AsNoTracking()
                .Where(update =>
                    ((update.Status == TelegramInboundUpdateStatus.Pending && update.NextAttemptAt <= now) ||
                     ((update.Status == TelegramInboundUpdateStatus.Processing ||
                       update.Status == TelegramInboundUpdateStatus.Prepared) &&
                      update.UpdatedAt <= staleBefore)) &&
                    !sets.Any(other =>
                        other.TelegramChatId == update.TelegramChatId &&
                        other.Id != update.Id &&
                        (other.Status == TelegramInboundUpdateStatus.Processing ||
                         other.Status == TelegramInboundUpdateStatus.Prepared) &&
                        other.UpdatedAt > staleBefore))
                .OrderBy(update => update.NextAttemptAt)
                .ThenBy(update => update.Id)
                .Select(update => (long?)update.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (candidateId is null)
            {
                return null;
            }

            var affected = await sets
                .Where(update => update.Id == candidateId &&
                    ((update.Status == TelegramInboundUpdateStatus.Pending && update.NextAttemptAt <= now) ||
                     ((update.Status == TelegramInboundUpdateStatus.Processing ||
                       update.Status == TelegramInboundUpdateStatus.Prepared) &&
                      update.UpdatedAt <= staleBefore)) &&
                    !sets.Any(other =>
                        other.TelegramChatId == update.TelegramChatId &&
                        other.Id != update.Id &&
                        (other.Status == TelegramInboundUpdateStatus.Processing ||
                         other.Status == TelegramInboundUpdateStatus.Prepared) &&
                        other.UpdatedAt > staleBefore))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(update => update.Status, TelegramInboundUpdateStatus.Processing)
                    .SetProperty(update => update.Attempts, update => update.Attempts + 1)
                    .SetProperty(update => update.UpdatedAt, now), cancellationToken);
            if (affected == 1)
            {
                context.ChangeTracker.Clear();
                return await sets.FirstAsync(update => update.Id == candidateId, cancellationToken);
            }
        }

        return null;
    }

    public async Task AddAsync(TelegramInboundUpdate update, CancellationToken cancellationToken) =>
        await context.Set<TelegramInboundUpdate>().AddAsync(update, cancellationToken);

    public Task UpdateAsync(TelegramInboundUpdate update, CancellationToken cancellationToken)
    {
        context.Set<TelegramInboundUpdate>().Update(update);
        return Task.CompletedTask;
    }
}
