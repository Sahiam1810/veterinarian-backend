using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramRegistrationSessionRepository
{
    Task<TelegramRegistrationSession?> GetActiveByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken);

    Task<TelegramRegistrationSession?> GetByCompletionTokenHashAsync(
        string completionTokenHash,
        CancellationToken cancellationToken);

    Task AddAsync(
        TelegramRegistrationSession session,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        TelegramRegistrationSession session,
        CancellationToken cancellationToken);
}
