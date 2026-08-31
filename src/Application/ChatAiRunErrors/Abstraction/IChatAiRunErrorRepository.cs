using ChatAiRunErrorEntity = Domain.ChatAiRunErrors.Entities.ChatAiRunError;

namespace Application.ChatAiRunErrors.Abstraction;

public interface IChatAiRunErrorRepository
{
    Task AddAsync(
        ChatAiRunErrorEntity chatAiRunError,
        CancellationToken cancellationToken = default);

    Task<ChatAiRunErrorEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatAiRunErrorEntity>> GetAllByChatAiRunIdAsync(
        Guid chatAiRunId,
        CancellationToken cancellationToken = default);
}
