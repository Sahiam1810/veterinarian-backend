using Application.ChatParticipants.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Infrastructure.ChatParticipants.Repository;

public sealed class ChatParticipantRepository : IChatParticipantRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatParticipantRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ChatParticipantEntity participant,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatParticipantEntity>().AddAsync(participant, cancellationToken);

    public Task<ChatParticipantEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatParticipantEntity>()
            .FirstOrDefaultAsync(participant => participant.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatParticipantEntity>> GetAllByConversationIdAsync(
        Guid chatConversationId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatParticipantEntity>()
            .AsNoTracking()
            .Where(participant => participant.ChatConversationId == chatConversationId)
            .OrderBy(participant => participant.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(
        ChatParticipantEntity participant,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatParticipantEntity>().Update(participant);
        return Task.CompletedTask;
    }
}
