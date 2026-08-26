using Domain.ConversationStatuses.Entities;

namespace Application.ConversationStatuses.Abstraction;

// Contrato de persistencia para estados de conversación.
public interface IConversationStatusRepository
{
    Task<IReadOnlyCollection<ConversationStatusEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<ConversationStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ConversationStatusEntity conversationStatus, CancellationToken cancellationToken);
    Task UpdateAsync(ConversationStatusEntity conversationStatus, CancellationToken cancellationToken);
    Task DeleteAsync(ConversationStatusEntity conversationStatus, CancellationToken cancellationToken);
}
