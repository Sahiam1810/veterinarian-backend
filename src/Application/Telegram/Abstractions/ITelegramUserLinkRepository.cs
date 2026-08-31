using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramUserLinkRepository
{
    Task<TelegramUserLink?> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken);

    Task<TelegramUserLink?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken);

    Task<TelegramUserLink?> GetByTelegramChatIdAsync(
        long telegramChatId,
        CancellationToken cancellationToken);

    Task AddAsync(TelegramUserLink link, CancellationToken cancellationToken);

    Task UpdateAsync(TelegramUserLink link, CancellationToken cancellationToken);
}
