using Domain.MessageTypes.Entities;

namespace Application.MessageTypes.Abstraction;

public interface IMessageTypeRepository
{
    Task<IReadOnlyCollection<MessageTypeEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<MessageTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(MessageTypeEntity messageType, CancellationToken cancellationToken);
    Task UpdateAsync(MessageTypeEntity messageType, CancellationToken cancellationToken);
    Task DeleteAsync(MessageTypeEntity messageType, CancellationToken cancellationToken);
}
