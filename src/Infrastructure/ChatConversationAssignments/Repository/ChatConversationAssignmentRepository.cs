using Application.ChatConversationAssignments.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Infrastructure.ChatConversationAssignments.Repository;

public sealed class ChatConversationAssignmentRepository : IChatConversationAssignmentRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatConversationAssignmentRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatConversationAssignmentEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationAssignmentEntity>()
            .AsNoTracking()
            .OrderBy(assignment => assignment.AssignedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatConversationAssignmentEntity?> GetByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatConversationAssignmentEntity>()
            .FirstOrDefaultAsync(
                assignment => assignment.ChatConversationId == chatConversationId,
                cancellationToken);

    public async Task<IReadOnlyCollection<ChatConversationAssignmentEntity>> GetByAgentHumanIdAsync(
        Guid agentHumanId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationAssignmentEntity>()
            .AsNoTracking()
            .Where(assignment => assignment.AgentHumanId == agentHumanId)
            .OrderBy(assignment => assignment.AssignedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatConversationAssignmentEntity>()
            .AnyAsync(
                assignment => assignment.ChatConversationId == chatConversationId,
                cancellationToken);

    public async Task AddAsync(
        ChatConversationAssignmentEntity assignment,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationAssignmentEntity>().AddAsync(assignment, cancellationToken);

    public Task UpdateAsync(
        ChatConversationAssignmentEntity assignment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatConversationAssignmentEntity>().Update(assignment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatConversationAssignmentEntity assignment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatConversationAssignmentEntity>().Remove(assignment);
        return Task.CompletedTask;
    }
}
