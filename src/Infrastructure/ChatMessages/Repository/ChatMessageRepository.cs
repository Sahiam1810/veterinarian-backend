using Application.ChatMessages.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Infrastructure.ChatMessages.Repository;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatMessageRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ChatMessageEntity message,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatMessageEntity>().AddAsync(message, cancellationToken);

    public Task<ChatMessageEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatMessageEntity>()
            .FirstOrDefaultAsync(message => message.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatMessageEntity>> GetAllByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatMessageEntity>()
            .AsNoTracking()
            .Where(message => message.ChatConversationId == chatConversationId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
}
