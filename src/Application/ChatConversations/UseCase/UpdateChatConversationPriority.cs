using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record UpdateChatConversationPriorityCommand(
    Guid Id,
    Guid? PriorityId) : IRequest<ChatConversationEntity>;

public sealed class UpdateChatConversationPriorityCommandHandler
    : IRequestHandler<UpdateChatConversationPriorityCommand, ChatConversationEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatConversationPriorityCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationEntity> Handle(
        UpdateChatConversationPriorityCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la conversación '{request.Id}'.");

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

        conversation.SetPriority(request.PriorityId);

        await _uow.ChatConversationsRepository.UpdateAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
