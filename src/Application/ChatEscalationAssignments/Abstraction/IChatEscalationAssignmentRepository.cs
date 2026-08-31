using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.Abstraction;

public interface IChatEscalationAssignmentRepository
{
    Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatEscalationAssignmentEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> GetByChatEscalationIdAsync(
        Guid chatEscalationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> GetByAgentHumanIdAsync(
        Guid agentHumanId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatEscalationAssignmentEntity assignment,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatEscalationAssignmentEntity assignment,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatEscalationAssignmentEntity assignment,
        CancellationToken cancellationToken = default);
}
