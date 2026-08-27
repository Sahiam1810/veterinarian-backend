using Application.AgentHumans.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Infrastructure.AgentHumans.Repository;

public sealed class AgentHumanRepository : IAgentHumanRepository
{
    private readonly VeterinaryDbContext _context;

    public AgentHumanRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<AgentHumanEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<AgentHumanEntity>()
            .AsNoTracking()
            .OrderBy(agent => agent.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<AgentHumanEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<AgentHumanEntity>()
            .FirstOrDefaultAsync(agent => agent.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<AgentHumanEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.Set<AgentHumanEntity>()
            .AsNoTracking()
            .Where(agent => agent.UserId == userId)
            .OrderBy(agent => agent.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        AgentHumanEntity agent,
        CancellationToken cancellationToken = default)
        => await _context.Set<AgentHumanEntity>().AddAsync(agent, cancellationToken);

    public Task UpdateAsync(
        AgentHumanEntity agent,
        CancellationToken cancellationToken = default)
    {
        _context.Set<AgentHumanEntity>().Update(agent);
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken) > 0;
}
