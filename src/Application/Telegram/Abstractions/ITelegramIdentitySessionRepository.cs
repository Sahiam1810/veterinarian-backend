using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramIdentitySessionRepository
{
    Task<TelegramIdentitySession?> GetCurrentByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken);

    Task<TelegramIdentitySession?> GetByPendingInboundUpdateIdAsync(
        long pendingInboundUpdateId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TelegramIdentitySession session,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        TelegramIdentitySession session,
        CancellationToken cancellationToken);
}
