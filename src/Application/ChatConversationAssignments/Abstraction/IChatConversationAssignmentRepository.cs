using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Application.ChatConversationAssignments.Abstraction;

public interface IChatConversationAssignmentRepository
{
    Task<IReadOnlyCollection<ChatConversationAssignmentEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatConversationAssignmentEntity?> GetByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatConversationAssignmentEntity>> GetByAgentHumanIdAsync(
        Guid agentHumanId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatConversationAssignmentEntity assignment,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatConversationAssignmentEntity assignment,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatConversationAssignmentEntity assignment,
        CancellationToken cancellationToken = default);
}
