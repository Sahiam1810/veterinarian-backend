using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramInboundUpdateRepository
{
    Task<bool> ExistsAsync(long updateId, CancellationToken cancellationToken);

    Task<TelegramInboundUpdate?> GetByIdAsync(
        long updateId,
        CancellationToken cancellationToken);

    Task<TelegramInboundUpdate?> ClaimNextAsync(
        DateTime now,
        CancellationToken cancellationToken);

    Task AddAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken);
}
