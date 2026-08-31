using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.Abstraction;

public interface IChatEscalationRepository
{
    Task<IReadOnlyCollection<ChatEscalationEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatEscalationEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatEscalationEntity>> GetByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatEscalationEntity escalation,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatEscalationEntity escalation,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatEscalationEntity escalation,
        CancellationToken cancellationToken = default);
}
