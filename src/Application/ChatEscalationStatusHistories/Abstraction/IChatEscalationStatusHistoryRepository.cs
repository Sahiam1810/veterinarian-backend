using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Application.ChatEscalationStatusHistories.Abstraction;

public interface IChatEscalationStatusHistoryRepository
{
    Task<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatEscalationStatusHistoryEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>> GetByChatEscalationIdAsync(
        Guid chatEscalationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatEscalationStatusHistoryEntity history,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatEscalationStatusHistoryEntity history,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatEscalationStatusHistoryEntity history,
        CancellationToken cancellationToken = default);
}
