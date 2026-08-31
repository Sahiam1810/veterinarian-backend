using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.ChatMessages.Abstraction;

public interface IChatMessageRepository
{
    Task AddAsync(
        ChatMessageEntity message,
        CancellationToken cancellationToken = default);

    Task<ChatMessageEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatMessageEntity>> GetAllByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default);
}
