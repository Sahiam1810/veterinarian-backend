using Application.ChatEscalations.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Infrastructure.ChatEscalations.Repository;

public sealed class ChatEscalationRepository : IChatEscalationRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatEscalationRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatEscalationEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationEntity>()
            .AsNoTracking()
            .OrderBy(escalation => escalation.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatEscalationEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatEscalationEntity>()
            .FirstOrDefaultAsync(escalation => escalation.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatEscalationEntity>> GetByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationEntity>()
            .AsNoTracking()
            .Where(escalation => escalation.ChatConversationId == chatConversationId)
            .OrderBy(escalation => escalation.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ChatEscalationEntity escalation,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationEntity>().AddAsync(escalation, cancellationToken);

    public Task UpdateAsync(
        ChatEscalationEntity escalation,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationEntity>().Update(escalation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatEscalationEntity escalation,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationEntity>().Remove(escalation);
        return Task.CompletedTask;
    }
}
