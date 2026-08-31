using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Application.ChatAttachments.UseCase;

public sealed record CreateChatAttachmentCommand(
    Guid ChatMessageId,
    string FileUrl,
    string FileType,
    string FileName) : IRequest<ChatAttachmentEntity>;

public sealed class CreateChatAttachmentCommandHandler
    : IRequestHandler<CreateChatAttachmentCommand, ChatAttachmentEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatAttachmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatAttachmentEntity> Handle(
        CreateChatAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var message = await _uow.ChatMessagesRepository.GetByIdAsync(
            request.ChatMessageId,
            cancellationToken);
        if (message is null)
        {
            throw new NotFoundException(
                $"No se encontró el mensaje '{request.ChatMessageId}'.");
        }

        var attachment = ChatAttachmentEntity.Create(
            request.ChatMessageId,
            request.FileUrl,
            request.FileType,
            request.FileName);

        await _uow.ChatAttachmentsRepository.AddAsync(attachment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return attachment;
    }
}
