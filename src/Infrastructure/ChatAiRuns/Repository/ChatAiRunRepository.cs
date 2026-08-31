using Application.ChatAiRuns.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Infrastructure.ChatAiRuns.Repository;

public sealed class ChatAiRunRepository : IChatAiRunRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatAiRunRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ChatAiRunEntity chatAiRun,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAiRunEntity>().AddAsync(chatAiRun, cancellationToken);

    public Task<ChatAiRunEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatAiRunEntity>()
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatAiRunEntity>> GetAllByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAiRunEntity>()
            .AsNoTracking()
            .Where(run => run.ChatConversationId == chatConversationId)
            .OrderBy(run => run.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(
        ChatAiRunEntity chatAiRun,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatAiRunEntity>().Update(chatAiRun);
        return Task.CompletedTask;
    }
}
