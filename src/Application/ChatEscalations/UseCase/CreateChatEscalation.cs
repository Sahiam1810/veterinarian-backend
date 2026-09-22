using Application.ChatConversations.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.Abstraction;
using MediatR;
using Microsoft.Extensions.Logging;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record CreateChatEscalationCommand(
    Guid ChatConversationId,
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    string? UpdateAt) : IRequest<ChatEscalationEntity>;

public sealed class CreateChatEscalationCommandHandler(
    IUnitOfWork uow,
    IChatConversationClientResolver clientResolver,
    IChatRealtimeNotifier chatRealtimeNotifier,
    ILogger<CreateChatEscalationCommandHandler> logger)
    : IRequestHandler<CreateChatEscalationCommand, ChatEscalationEntity>
{
    public async Task<ChatEscalationEntity> Handle(
        CreateChatEscalationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await uow.ChatConversationsRepository.GetByIdAsync(
            request.ChatConversationId,
            cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException(
                $"No se encontró la conversación '{request.ChatConversationId}'.");
        }

        var status = await uow.EscalationStatusesRepository.GetByIdAsync(
            request.EscalationStatusId,
            cancellationToken);
        if (status is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de escalamiento '{request.EscalationStatusId}'.");
        }

        var escalation = ChatEscalationEntity.Create(
            request.ChatConversationId,
            request.EscalationStatusId,
            request.FromAi,
            request.Reason,
            request.UpdateAt);

        await uow.ChatEscalationsRepository.AddAsync(escalation, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        // Ticket B5: la bandeja de la Recepcionista ya está guardada en este
        // punto — un problema al armar o enviar el broadcast nunca debe hacer
        // fallar la creación del escalamiento en sí.
        try
        {
            var payload = await BuildCreatedPayloadAsync(
                escalation, conversation, status.Name.Value, cancellationToken);
            await chatRealtimeNotifier.NotifyEscalationCreatedAsync(payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Failed to broadcast ChatEscalationCreated for escalation {EscalationId}.",
                escalation.Id);
        }

        return escalation;
    }

    private async Task<ChatEscalationCreatedPayload> BuildCreatedPayloadAsync(
        ChatEscalationEntity escalation,
        ChatConversationEntity conversation,
        string statusName,
        CancellationToken cancellationToken)
    {
        string? priority = null;

        var clientInfo = await clientResolver.ResolveAsync(
            escalation.ChatConversationId, cancellationToken);

        return new ChatEscalationCreatedPayload(
            escalation.Id,
            escalation.ChatConversationId,
            clientInfo.ClientId,
            clientInfo.ClientName,
            clientInfo.ClientPhone,
            escalation.Reason,
            priority,
            statusName,
            conversation.Channel,
            escalation.CreatedAt,
            escalation.Reason);
    }
}
