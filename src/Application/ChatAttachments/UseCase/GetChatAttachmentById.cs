using Application.Common.Abstractions;
using MediatR;
using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Application.ChatAttachments.UseCase;

public sealed record GetChatAttachmentByIdQuery(Guid Id)
    : IRequest<ChatAttachmentEntity?>;

public sealed class GetChatAttachmentByIdQueryHandler
    : IRequestHandler<GetChatAttachmentByIdQuery, ChatAttachmentEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatAttachmentByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatAttachmentEntity?> Handle(
        GetChatAttachmentByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAttachmentsRepository.GetByIdAsync(request.Id, cancellationToken);
}
