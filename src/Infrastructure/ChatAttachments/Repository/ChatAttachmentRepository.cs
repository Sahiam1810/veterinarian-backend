using Application.ChatAttachments.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Infrastructure.ChatAttachments.Repository;

public sealed class ChatAttachmentRepository : IChatAttachmentRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatAttachmentRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ChatAttachmentEntity attachment,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAttachmentEntity>().AddAsync(attachment, cancellationToken);

    public Task<ChatAttachmentEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatAttachmentEntity>()
            .FirstOrDefaultAsync(attachment => attachment.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatAttachmentEntity>> GetAllByMessageIdAsync(
        Guid chatMessageId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAttachmentEntity>()
            .AsNoTracking()
            .Where(attachment => attachment.ChatMessageId == chatMessageId)
            .OrderBy(attachment => attachment.CreatedAt)
            .ToListAsync(cancellationToken);
}
