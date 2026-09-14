using System.Security.Cryptography;
using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Application.ChatEscalations.UseCase;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Linking;
using Application.Telegram.Messages;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Telegram.Processing;

public sealed record ProcessTelegramUpdateCommand(long UpdateId) : IRequest;

public sealed class ProcessTelegramUpdateHandler(
    ITelegramUnitOfWork unitOfWork,
    IConversationContextProvider conversationContextProvider,
    IAgentMessageDispatcher dispatcher,
    IAgentDelegatedIdentityProvider identityProvider,
    ITelegramBotClient botClient,
    ISender sender,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider,
    ILogger<ProcessTelegramUpdateHandler> logger) : IRequestHandler<ProcessTelegramUpdateCommand>
{
    private const string GuestAccessDisabledReply =
        "Por el momento este canal no está disponible para consultas. Intenta más tarde.";
    private const string GuestStartReply =
        "¡Hola! Puedes hacer preguntas generales sobre Huellitas y sus servicios. " +
        "Si quieres agendar una cita o registrar una mascota, te pediré tu nombre, cédula, " +
        "correo y teléfono para registrarte.";
    private const string EscalationConfirmedReply =
        "Tu conversación está siendo atendida por un asesor.";
    // Decisión 1 (Ticket B2): un invitado sin vincular nunca escala directamente.
    // Se le redirige al mismo flujo conversacional de identificación ya existente
    // (Ticket 3, en el chatbot) en vez de recolectar sus datos aquí.
    private const string GuestEscalationRedirectReply =
        "Para conectarte con un asesor, primero cuéntame qué necesitas — por ejemplo, " +
        "agendar una cita o registrar una mascota — así puedo identificarte.";

    // Coincidencia simple por substring, mismo criterio ya usado en el resto del
    // proyecto (p. ej. las reglas del enrutador del chatbot). No distingue mayúsculas.
    private static readonly string[] EscalationPhrases =
    [
        "asesor",
        "hablar con alguien",
        "hablar con una persona",
        "hablar con un humano",
        "hablar con un agente",
        "atencion humana",
        "atención humana",
        "quiero un humano",
        "necesito un humano",
    ];

    public async Task Handle(
        ProcessTelegramUpdateCommand request,
        CancellationToken cancellationToken)
    {
        var update = await unitOfWork.InboundUpdatesRepository.GetByIdAsync(
            request.UpdateId,
            cancellationToken);
        if (update is null || update.Status != TelegramInboundUpdateStatus.Processing)
        {
            return;
        }

        var messageText = update.MessageText;
        try
        {
            if (!string.IsNullOrWhiteSpace(update.ResponseText))
            {
                await DeliverAsync(update, update.ResponseText, cancellationToken);
                return;
            }

            if (!string.Equals(update.ChatType, "private", StringComparison.Ordinal))
            {
                await DeliverAsync(update, "Por el momento solo atiendo chats privados.", cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                await DeliverAsync(update, "Por el momento solo puedo procesar mensajes de texto.", cancellationToken);
                return;
            }

            if (messageText.StartsWith("/start ", StringComparison.Ordinal))
            {
                await ProcessLinkCodeAsync(update, messageText[7..].Trim(), cancellationToken);
                return;
            }

            if (string.Equals(messageText, "/start", StringComparison.OrdinalIgnoreCase))
            {
                await DeliverAsync(update, GuestStartReply, cancellationToken);
                return;
            }

            var userLink = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
                update.TelegramUserId,
                cancellationToken);
            if (userLink is null)
            {
                if (settings.GuestModeEnabled)
                {
                    // Decisión 1 (Ticket B2): un invitado nunca escala directamente —
                    // se le redirige al flujo de identificación conversacional ya
                    // existente (Ticket 3). No se toca CHAT_ESCALATIONS ni CHAT_CONVERSATIONS.
                    if (IsEscalationRequest(messageText))
                    {
                        await DeliverAsync(update, GuestEscalationRedirectReply, cancellationToken);
                        return;
                    }

                    // Sin verificación de identidad: la respuesta del agente se
                    // entrega tal cual, sin importar AccessRequirement. Si el
                    // mensaje requiere datos del cliente (agendar cita, registrar
                    // mascota), el propio agente los recolecta en la conversación
                    // y registra al cliente directamente (ver módulo de agendamiento).
                    var guestResult = await DispatchGuestMessageAsync(
                        update,
                        messageText,
                        cancellationToken);
                    await DeliverAsync(update, ResponseText(guestResult), cancellationToken);
                    return;
                }

                await DeliverAsync(
                    update,
                    GuestAccessDisabledReply,
                    cancellationToken);
                return;
            }

            if (IsEscalationRequest(messageText))
            {
                await EscalateAsync(userLink, messageText, cancellationToken);
                await DeliverAsync(update, EscalationConfirmedReply, cancellationToken);
                return;
            }

            var result = await DispatchAuthenticatedAsync(
                userLink,
                messageText,
                $"telegram-update-{update.Id}-verified",
                update.Id,
                cancellationToken);
            await DeliverAsync(update, ResponseText(result), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var errorCode = SafeErrorCode(exception);
            logger.LogWarning(
                exception,
                "Telegram update processing failed with code {ErrorCode} on attempt {Attempt}.",
                errorCode,
                update.Attempts);
            update.ScheduleRetry(
                now.AddSeconds(Math.Pow(2, Math.Max(0, update.Attempts - 1))),
                errorCode,
                settings.MaxProcessingAttempts,
                now);
            await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AgentMessageResult> DispatchGuestMessageAsync(
        TelegramInboundUpdate update,
        string messageText,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"telegram-update-{update.Id}-guest";
        var identity = identityProvider.GetGuest(update.TelegramUserId);
        var context = new AgentConversationContext(
            CreateGuestConversationId(update.TelegramChatId),
            "telegram",
            false);
        return await dispatcher.DispatchAsync(
            new AgentMessageDispatchRequest(
                messageText,
                identity.PersonId,
                null,
                "es-CO",
                identity.Role,
                idempotencyKey,
                CreateCorrelationId(update.Id)),
            context,
            identity.AccessToken,
            cancellationToken);
    }

    private async Task<AgentMessageResult> DispatchAuthenticatedAsync(
        TelegramUserLink userLink,
        string messageText,
        string idempotencyKey,
        long correlationSourceId,
        CancellationToken cancellationToken)
    {
        var context = await ResolveConversationAsync(userLink, idempotencyKey, cancellationToken);
        var identity = await identityProvider.GetAsync(userLink.PersonId, cancellationToken);
        var result = await dispatcher.DispatchAsync(
            new AgentMessageDispatchRequest(
                messageText,
                identity.PersonId,
                null,
                "es-CO",
                identity.Role,
                idempotencyKey,
                CreateCorrelationId(correlationSourceId)),
            context with { Channel = "telegram" },
            identity.AccessToken,
            cancellationToken);
        if (result.AccessRequirement != AgentAccessRequirement.None)
        {
            throw new AgentContractException();
        }

        return result;
    }

    private static bool IsEscalationRequest(string messageText) =>
        EscalationPhrases.Any(phrase =>
            messageText.Contains(phrase, StringComparison.OrdinalIgnoreCase));

    // Crea el ChatEscalation para un cliente ya vinculado. Reutiliza
    // ResolveConversationAsync (ya existente) para obtener la ChatConversation
    // real de la persona y, con ella, si ya hay un escalamiento activo sin
    // resolver (AgentConversationContext.IsEscalated, calculado por el mismo
    // IActiveConversationEscalationReader que usa PersistentConversationContextProvider)
    // — evita crear un segundo registro si el cliente repite la frase.
    private async Task EscalateAsync(
        TelegramUserLink userLink,
        string messageText,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"telegram-update-{userLink.Id}-escalation";
        var context = await ResolveConversationAsync(userLink, idempotencyKey, cancellationToken);
        if (context.IsEscalated)
        {
            return;
        }

        await sender.Send(
            new CreateChatEscalationCommand(
                context.ConversationId,
                settings.PendingEscalationStatusId,
                FromAi: false,
                Reason: messageText,
                UpdateAt: null),
            cancellationToken);
    }

    private static string ResponseText(AgentMessageResult result) =>
        string.IsNullOrWhiteSpace(result.Message)
            ? "Tu conversación está siendo atendida por un asesor."
            : result.Message;

    private async Task ProcessLinkCodeAsync(
        TelegramInboundUpdate update,
        string code,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(
                new ConsumeTelegramLinkCodeCommand(code, update.TelegramUserId, update.TelegramChatId),
                cancellationToken);
            await DeliverAsync(update, "Tu cuenta de Huellitas quedó vinculada correctamente.", cancellationToken);
        }
        catch (TelegramIntegrationException exception) when (
            exception is TelegramLinkCodeInvalidException
                or TelegramIdentityConflictException
                or TelegramAccountUnavailableException)
        {
            await DeliverAsync(update, "El código de vinculación es inválido, ya fue usado o venció.", cancellationToken);
        }
    }

    private async Task<AgentConversationContext> ResolveConversationAsync(
        TelegramUserLink userLink,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var binding = await unitOfWork.ConversationLinksRepository.GetBindingAsync(
            userLink.Id,
            cancellationToken);
        if (binding is { Closed: false })
        {
            return await conversationContextProvider.ResolveAsync(
                userLink.PersonId,
                binding.ConversationId,
                idempotencyKey,
                cancellationToken);
        }

        AgentConversationContext? created = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            created = await conversationContextProvider.ResolveAsync(
                userLink.PersonId,
                null,
                idempotencyKey,
                transactionToken);
            var link = await unitOfWork.ConversationLinksRepository.GetByUserLinkIdAsync(
                userLink.Id,
                transactionToken);
            if (link is null)
            {
                link = TelegramConversationLink.Create(
                    userLink.Id,
                    created.ConversationId,
                    timeProvider.GetUtcNow().UtcDateTime);
                await unitOfWork.ConversationLinksRepository.AddAsync(link, transactionToken);
            }
            else
            {
                link.BindConversation(created.ConversationId, timeProvider.GetUtcNow().UtcDateTime);
                await unitOfWork.ConversationLinksRepository.UpdateAsync(link, transactionToken);
            }
        }, cancellationToken);
        return created!;
    }

    private async Task DeliverAsync(
        TelegramInboundUpdate update,
        string text,
        CancellationToken cancellationToken)
    {
        update.PrepareResponse(text, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var chunks = TelegramTextChunker.Split(text);
        for (var index = update.LastSentChunkIndex + 1; index < chunks.Count; index++)
        {
            await botClient.SendTextAsync(update.TelegramChatId, chunks[index], cancellationToken);
            update.ConfirmChunk(index, timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        update.Complete(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Guid CreateCorrelationId(long updateId)
    {
        var hash = SHA256.HashData(BitConverter.GetBytes(updateId));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static Guid CreateGuestConversationId(long telegramChatId)
    {
        var hash = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(
                $"huellitas:telegram:guest:conversation:{telegramChatId}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string SafeErrorCode(Exception exception) => exception switch
    {
        TelegramDeliveryException => "telegram_delivery_failed",
        AgentGatewayException => "agent_request_failed",
        TelegramIntegrationException => "telegram_processing_failed",
        _ => "unexpected_processing_failure"
    };
}
