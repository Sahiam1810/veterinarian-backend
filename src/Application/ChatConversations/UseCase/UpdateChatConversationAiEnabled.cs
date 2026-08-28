using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record UpdateChatConversationAiEnabledCommand(
    Guid Id,
    bool AiEnabled) : IRequest<ChatConversationEntity>;

public sealed class UpdateChatConversationAiEnabledCommandHandler
    : IRequestHandler<UpdateChatConversationAiEnabledCommand, ChatConversationEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatConversationAiEnabledCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationEntity> Handle(
        UpdateChatConversationAiEnabledCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la conversación '{request.Id}'.");

        conversation.SetAiEnabled(request.AiEnabled);

        await _uow.ChatConversationsRepository.UpdateAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
