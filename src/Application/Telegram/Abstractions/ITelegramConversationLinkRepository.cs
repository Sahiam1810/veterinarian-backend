using Domain.Telegram.Entities;

namespace Application.Telegram.Abstractions;

public interface ITelegramConversationLinkRepository
{
    Task<TelegramConversationBinding?> GetBindingAsync(
        Guid telegramUserLinkId,
        CancellationToken cancellationToken);

    Task<TelegramConversationLink?> GetByUserLinkIdAsync(
        Guid telegramUserLinkId,
        CancellationToken cancellationToken);

    // Ticket B4: búsqueda inversa ChatConversationId -> TelegramConversationLink,
    // usada al reenviar la respuesta de un asesor humano a Telegram.
    Task<TelegramConversationLink?> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    Task AddAsync(
        TelegramConversationLink link,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        TelegramConversationLink link,
        CancellationToken cancellationToken);
}

public sealed record TelegramConversationBinding(
    Guid ConversationId,
    bool Closed,
    DateTime? LastMessageAt,
    DateTime CreatedAt);

