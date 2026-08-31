using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Application.ChatParticipants.Abstraction;

public interface IChatParticipantRepository
{
    Task AddAsync(
        ChatParticipantEntity participant,
        CancellationToken cancellationToken = default);

    Task<ChatParticipantEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatParticipantEntity>> GetAllByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatParticipantEntity participant,
        CancellationToken cancellationToken = default);
}
