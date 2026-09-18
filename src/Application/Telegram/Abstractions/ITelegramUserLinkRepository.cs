using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramUserLinkRepository
{
    // Ticket B4: segundo salto de la búsqueda inversa hacia el chat de Telegram.
    Task<TelegramUserLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

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
