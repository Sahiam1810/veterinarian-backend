using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record UpdateChatConversationStatusCommand(
    Guid Id,
    Guid ConversationStatusId) : IRequest<ChatConversationEntity>;

public sealed class UpdateChatConversationStatusCommandHandler
    : IRequestHandler<UpdateChatConversationStatusCommand, ChatConversationEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatConversationStatusCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationEntity> Handle(
        UpdateChatConversationStatusCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la conversación '{request.Id}'.");

        var status = await _uow.ConversationStatusesRepository.GetByIdAsync(
            request.ConversationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de conversación '{request.ConversationStatusId}'.");
        }

        conversation.ChangeStatus(request.ConversationStatusId);

        await _uow.ChatConversationsRepository.UpdateAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
