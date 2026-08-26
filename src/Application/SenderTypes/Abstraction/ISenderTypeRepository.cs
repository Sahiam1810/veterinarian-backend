using Domain.SenderTypes.Entities;

namespace Application.SenderTypes.Abstraction;

public interface ISenderTypeRepository
{
    Task<IReadOnlyCollection<SenderTypeEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<SenderTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(SenderTypeEntity senderType, CancellationToken cancellationToken);
    Task UpdateAsync(SenderTypeEntity senderType, CancellationToken cancellationToken);
    Task DeleteAsync(SenderTypeEntity senderType, CancellationToken cancellationToken);
}
