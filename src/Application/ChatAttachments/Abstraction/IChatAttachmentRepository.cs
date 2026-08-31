using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Application.ChatAttachments.Abstraction;

public interface IChatAttachmentRepository
{
    Task AddAsync(
        ChatAttachmentEntity attachment,
        CancellationToken cancellationToken = default);

    Task<ChatAttachmentEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatAttachmentEntity>> GetAllByMessageIdAsync(
        Guid chatMessageId,
        CancellationToken cancellationToken = default);
}
