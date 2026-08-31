using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.Abstraction;

public interface IChatEscalationResolutionRepository
{
    Task<IReadOnlyCollection<ChatEscalationResolutionEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatEscalationResolutionEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatEscalationResolutionEntity>> GetByChatEscalationIdAsync(
        Guid chatEscalationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatEscalationResolutionEntity resolution,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatEscalationResolutionEntity resolution,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatEscalationResolutionEntity resolution,
        CancellationToken cancellationToken = default);
}
