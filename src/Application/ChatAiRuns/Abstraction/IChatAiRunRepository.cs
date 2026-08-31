using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Application.ChatAiRuns.Abstraction;

public interface IChatAiRunRepository
{
    Task AddAsync(
        ChatAiRunEntity chatAiRun,
        CancellationToken cancellationToken = default);

    Task<ChatAiRunEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatAiRunEntity>> GetAllByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatAiRunEntity chatAiRun,
        CancellationToken cancellationToken = default);
}
