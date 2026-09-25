using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record CreateChatConversationCommand(
    bool AiEnabled = true,
    string Channel = "Web") : IRequest<ChatConversationEntity>;

public sealed class CreateChatConversationCommandHandler
    : IRequestHandler<CreateChatConversationCommand, ChatConversationEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatConversationCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationEntity> Handle(
        CreateChatConversationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = ChatConversationEntity.Create(
            request.AiEnabled,
            request.Channel);

        await _uow.ChatConversationsRepository.AddAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
