using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramLinkingSessionRepository
{
    Task<TelegramLinkingSession?> GetActiveByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TelegramLinkingSession session,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        TelegramLinkingSession session,
        CancellationToken cancellationToken);
}
