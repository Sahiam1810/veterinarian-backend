using Application.ChatEscalationResolutions.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Infrastructure.ChatEscalationResolutions.Repository;

public sealed class ChatEscalationResolutionRepository : IChatEscalationResolutionRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatEscalationResolutionRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatEscalationResolutionEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationResolutionEntity>()
            .AsNoTracking()
            .OrderBy(resolution => resolution.ResolvedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatEscalationResolutionEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatEscalationResolutionEntity>()
            .FirstOrDefaultAsync(resolution => resolution.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatEscalationResolutionEntity>> GetByChatEscalationIdAsync(
        Guid chatEscalationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationResolutionEntity>()
            .AsNoTracking()
            .Where(resolution => resolution.ChatEscalationId == chatEscalationId)
            .OrderBy(resolution => resolution.ResolvedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ChatEscalationResolutionEntity resolution,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationResolutionEntity>().AddAsync(resolution, cancellationToken);

    public Task UpdateAsync(
        ChatEscalationResolutionEntity resolution,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationResolutionEntity>().Update(resolution);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatEscalationResolutionEntity resolution,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationResolutionEntity>().Remove(resolution);
        return Task.CompletedTask;
    }
}
