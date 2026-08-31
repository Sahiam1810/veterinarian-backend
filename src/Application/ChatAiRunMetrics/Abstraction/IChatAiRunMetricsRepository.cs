using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;

namespace Application.ChatAiRunMetrics.Abstraction;

public interface IChatAiRunMetricsRepository
{
    Task AddAsync(
        ChatAiRunMetricsEntity chatAiRunMetrics,
        CancellationToken cancellationToken = default);

    Task<ChatAiRunMetricsEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ChatAiRunMetricsEntity?> GetByChatAiRunIdAsync(
        Guid chatAiRunId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByChatAiRunIdAsync(
        Guid chatAiRunId,
        CancellationToken cancellationToken = default);
}
