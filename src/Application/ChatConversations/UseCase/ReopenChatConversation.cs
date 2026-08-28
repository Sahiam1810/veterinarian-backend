using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record ReopenChatConversationCommand(Guid Id) : IRequest<ChatConversationEntity>;

public sealed class ReopenChatConversationCommandHandler
    : IRequestHandler<ReopenChatConversationCommand, ChatConversationEntity>
{
    private readonly IUnitOfWork _uow;

    public ReopenChatConversationCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationEntity> Handle(
        ReopenChatConversationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la conversación '{request.Id}'.");

        conversation.Reopen();

        await _uow.ChatConversationsRepository.UpdateAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
