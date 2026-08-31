using Application.ChatEscalationStatusHistories.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Infrastructure.ChatEscalationStatusHistories.Repository;

public sealed class ChatEscalationStatusHistoryRepository : IChatEscalationStatusHistoryRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatEscalationStatusHistoryRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationStatusHistoryEntity>()
            .AsNoTracking()
            .OrderBy(history => history.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatEscalationStatusHistoryEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatEscalationStatusHistoryEntity>()
            .FirstOrDefaultAsync(history => history.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>> GetByChatEscalationIdAsync(
        Guid chatEscalationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationStatusHistoryEntity>()
            .AsNoTracking()
            .Where(history => history.ChatEscalationId == chatEscalationId)
            .OrderBy(history => history.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ChatEscalationStatusHistoryEntity history,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatEscalationStatusHistoryEntity>().AddAsync(history, cancellationToken);

    public Task UpdateAsync(
        ChatEscalationStatusHistoryEntity history,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationStatusHistoryEntity>().Update(history);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatEscalationStatusHistoryEntity history,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatEscalationStatusHistoryEntity>().Remove(history);
        return Task.CompletedTask;
    }
}
