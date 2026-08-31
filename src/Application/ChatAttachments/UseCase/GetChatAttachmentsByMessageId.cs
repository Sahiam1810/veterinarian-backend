using Application.Common.Abstractions;
using MediatR;
using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Application.ChatAttachments.UseCase;

public sealed record GetChatAttachmentsByMessageIdQuery(Guid ChatMessageId)
    : IRequest<IReadOnlyCollection<ChatAttachmentEntity>>;

public sealed class GetChatAttachmentsByMessageIdQueryHandler
    : IRequestHandler<GetChatAttachmentsByMessageIdQuery, IReadOnlyCollection<ChatAttachmentEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatAttachmentsByMessageIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatAttachmentEntity>> Handle(
        GetChatAttachmentsByMessageIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAttachmentsRepository.GetAllByMessageIdAsync(
            request.ChatMessageId,
            cancellationToken);
}
