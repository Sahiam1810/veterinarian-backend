using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.Abstraction;

public interface IChatConversationRepository
{
    Task<IReadOnlyCollection<ChatConversationEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatConversationEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatConversationEntity conversation,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatConversationEntity conversation,
        CancellationToken cancellationToken = default);
}
