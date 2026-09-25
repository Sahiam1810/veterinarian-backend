using Application.Agent.Abstractions;
using Application.ChatMessages.Events;
using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.ChatMessages.Notifications;

// Ticket B5: mismo evento genérico del que ya se suscribe
// ForwardHumanChatMessageToTelegramHandler (Ticket B4) — el módulo
// ChatMessages sigue sin saber nada de SignalR ni de Telegram. Solo se
// reenvía a la bandeja si la conversación tiene un escalamiento activo
// (evita ruido de conversaciones no atendidas por un humano).
public sealed class NotifyReceptionOfNewChatMessageHandler(
    IUnitOfWork uow,
    IActiveConversationEscalationReader escalationReader,
    IChatRealtimeNotifier chatRealtimeNotifier,
    ILogger<NotifyReceptionOfNewChatMessageHandler> logger)
    : INotificationHandler<ChatMessageCreatedNotification>
{
    public async Task Handle(
        ChatMessageCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        var message = notification.Message;

        try
        {
            var hasActiveEscalation = await escalationReader.HasActiveAsync(
                message.ChatConversationId, cancellationToken);
            if (!hasActiveEscalation)
            {
                return;
            }

            var senderType = await uow.SenderTypesRepository.GetByIdAsync(
                message.SenderTypesId, cancellationToken);

            var payload = new ChatMessageReceivedPayload(
                message.Id,
                message.ChatConversationId,
                senderType?.Name.Value ?? message.SenderTypesId.ToString(),
                SenderName: null,
                message.Content,
                message.CreatedAt,
                MessageType: null);

            await chatRealtimeNotifier.NotifyMessageReceivedAsync(payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // El mensaje ya está persistido — un fallo de broadcast no debe
            // afectar la respuesta HTTP que ya recibió quien lo envió.
            logger.LogError(
                exception,
                "Failed to broadcast ChatMessageReceived for message {MessageId}.",
                message.Id);
        }
    }
}
