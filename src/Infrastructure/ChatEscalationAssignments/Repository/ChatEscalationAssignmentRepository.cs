using Application.ChatEscalationAssignments.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Infrastructure.ChatEscalationAssignments.Repository;

public sealed class ChatEscalationAssignmentRepository : IChatEscalationAssignmentRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatEscalationAssignmentRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationAssignmentEntity>()
            .AsNoTracking()
            .OrderBy(assignment => assignment.AssignedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatEscalationAssignmentEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatEscalationAssignmentEntity>()
            .FirstOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> GetByChatEscalationIdAsync(
        Guid chatEscalationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationAssignmentEntity>()
            .AsNoTracking()
            .Where(assignment => assignment.ChatEscalationId == chatEscalationId)
            .OrderBy(assignment => assignment.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> GetByAgentHumanIdAsync(
        Guid agentHumanId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationAssignmentEntity>()
            .AsNoTracking()
            .Where(assignment => assignment.AgentHumanId == agentHumanId)
            .OrderBy(assignment => assignment.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ChatEscalationAssignmentEntity assignment,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationAssignmentEntity>().AddAsync(assignment, cancellationToken);

    public Task UpdateAsync(
        ChatEscalationAssignmentEntity assignment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationAssignmentEntity>().Update(assignment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatEscalationAssignmentEntity assignment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationAssignmentEntity>().Remove(assignment);
        return Task.CompletedTask;
    }
}
