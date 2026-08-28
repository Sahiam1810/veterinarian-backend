using Application.ChatConversations.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Infrastructure.ChatConversations.Repository;

public sealed class ChatConversationRepository : IChatConversationRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatConversationRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatConversationEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationEntity>()
            .AsNoTracking()
            .OrderBy(conversation => conversation.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatConversationEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatConversationEntity>()
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);

    public async Task AddAsync(
        ChatConversationEntity conversation,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationEntity>().AddAsync(conversation, cancellationToken);

    public Task UpdateAsync(
        ChatConversationEntity conversation,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatConversationEntity>().Update(conversation);
        return Task.CompletedTask;
    }
}
