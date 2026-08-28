using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record CloseChatConversationCommand(
    Guid Id,
    Guid? ClosedBy = null) : IRequest<ChatConversationEntity>;

public sealed class CloseChatConversationCommandHandler
    : IRequestHandler<CloseChatConversationCommand, ChatConversationEntity>
{
    private readonly IUnitOfWork _uow;

    public CloseChatConversationCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationEntity> Handle(
        CloseChatConversationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la conversación '{request.Id}'.");

        conversation.Close(request.ClosedBy);

        await _uow.ChatConversationsRepository.UpdateAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
