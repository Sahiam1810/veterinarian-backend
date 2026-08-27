using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.Abstraction;

public interface IAgentHumanRepository
{
    Task<IReadOnlyCollection<AgentHumanEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<AgentHumanEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AgentHumanEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AgentHumanEntity agent,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        AgentHumanEntity agent,
        CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
