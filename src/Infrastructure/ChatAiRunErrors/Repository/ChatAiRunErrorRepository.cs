using Application.ChatAiRunErrors.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatAiRunErrorEntity = Domain.ChatAiRunErrors.Entities.ChatAiRunError;

namespace Infrastructure.ChatAiRunErrors.Repository;

public sealed class ChatAiRunErrorRepository : IChatAiRunErrorRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatAiRunErrorRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ChatAiRunErrorEntity chatAiRunError,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAiRunErrorEntity>().AddAsync(chatAiRunError, cancellationToken);

    public Task<ChatAiRunErrorEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatAiRunErrorEntity>()
            .FirstOrDefaultAsync(error => error.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatAiRunErrorEntity>> GetAllByChatAiRunIdAsync(
        Guid chatAiRunId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAiRunErrorEntity>()
            .AsNoTracking()
            .Where(error => error.ChatAiRunId == chatAiRunId)
            .OrderBy(error => error.CreatedAt)
            .ToListAsync(cancellationToken);
}
