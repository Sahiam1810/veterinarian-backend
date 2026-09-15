using Application.ChatMessages.Events;
using Application.Telegram.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Telegram.Notifications;

// Ticket B4: cierra el círculo asesor humano -> Telegram. El módulo ChatMessages
// no sabe nada de Telegram — publica ChatMessageCreatedNotification para
// cualquier remitente; esta suscripción decide si le corresponde actuar.
//
// Con la Decisión 1 (Ticket B2) ya tomada, toda conversación que llega hasta
// aquí con un mensaje de "Agente humano" pertenece a un cliente vinculado, que
// siempre tiene un TelegramConversationLink real (creado por
// ProcessTelegramUpdateHandler.ResolveConversationAsync). Si no se encuentra,
// es un estado inconsistente real — se registra como error, no como un caso
// esperado a manejar con gracia.
public sealed class ForwardHumanChatMessageToTelegramHandler(
    ITelegramConversationLinkRepository conversationLinks,
    ITelegramUserLinkRepository userLinks,
    ITelegramBotClient botClient,
    ITelegramRuntimeSettings settings,
    ILogger<ForwardHumanChatMessageToTelegramHandler> logger)
    : INotificationHandler<ChatMessageCreatedNotification>
{
    public async Task Handle(
        ChatMessageCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        var message = notification.Message;
        if (message.SenderTypesId != settings.HumanAgentSenderTypeId)
        {
            // Mensajes de Cliente o Agente IA no se reenvían — evita el eco.
            return;
        }

        try
        {
            var conversationLink = await conversationLinks.GetByConversationIdAsync(
                message.ChatConversationId,
                cancellationToken);
            if (conversationLink is null)
            {
                logger.LogError(
                    "No TelegramConversationLink found for conversation {ConversationId}; " +
                    "cannot forward human agent message {MessageId}.",
                    message.ChatConversationId,
                    message.Id);
                return;
            }

            var userLink = await userLinks.GetByIdAsync(
                conversationLink.TelegramUserLinkId,
                cancellationToken);
            if (userLink is null)
            {
                logger.LogError(
                    "No TelegramUserLink '{TelegramUserLinkId}' found for conversation " +
                    "{ConversationId}; cannot forward human agent message {MessageId}.",
                    conversationLink.TelegramUserLinkId,
                    message.ChatConversationId,
                    message.Id);
                return;
            }

            await botClient.SendTextAsync(userLink.TelegramChatId, message.Content, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // El mensaje ya está persistido en CHAT_MESSAGES en este punto — un
            // fallo de entrega en tiempo real no debe tumbar la respuesta HTTP
            // que ya recibió la Recepcionista.
            logger.LogError(
                exception,
                "Failed to forward human agent message {MessageId} to Telegram for conversation {ConversationId}.",
                message.Id,
                message.ChatConversationId);
        }
    }
}
