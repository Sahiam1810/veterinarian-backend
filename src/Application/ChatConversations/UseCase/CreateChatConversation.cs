using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record CreateChatConversationCommand(
    Guid ConversationStatusId,
    Guid? PriorityId,
    bool AiEnabled = true) : IRequest<ChatConversationEntity>;

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
        var status = await _uow.ConversationStatusesRepository.GetByIdAsync(
            request.ConversationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de conversación '{request.ConversationStatusId}'.");
        }

        if (request.PriorityId.HasValue)
        {
            var priority = await _uow.PrioritiesRepository.GetByIdAsync(
                request.PriorityId.Value,
                cancellationToken);
            if (priority is null)
            {
                throw new NotFoundException(
                    $"No se encontró la prioridad '{request.PriorityId.Value}'.");
            }
        }

        var conversation = ChatConversationEntity.Create(
            request.ConversationStatusId,
            request.PriorityId,
            request.AiEnabled);

        await _uow.ChatConversationsRepository.AddAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
