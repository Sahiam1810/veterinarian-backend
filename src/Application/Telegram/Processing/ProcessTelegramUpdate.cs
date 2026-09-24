using System.Security.Cryptography;
using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Application.ChatConversations.UseCase;
using Application.ChatEscalations.UseCase;
using Application.ChatMessages.UseCase;
using Application.ChatParticipants.UseCase;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
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
    IAgentConversationDefaults conversationDefaults,
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
    // SENDER_TYPES "Agente IA" (seed 82000000-...-0002). Hardcodeado con el mismo
    // criterio que Telegram__PendingEscalationStatusId / el id de "Cliente".
    private static readonly Guid AiAgentSenderTypeId =
        Guid.Parse("82000000-0000-0000-0000-000000000002");

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

            // Todo /start (con o sin payload) responde el mismo mensaje de bienvenida.
            if (string.Equals(messageText, "/start", StringComparison.OrdinalIgnoreCase) ||
                messageText.StartsWith("/start ", StringComparison.OrdinalIgnoreCase))
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
                    //
                    // Tras un registro/vinculación conversacional exitoso, el agente
                    // puede devolver resumeMessage. En ese caso el link ya existe:
                    // reenviamos el intent canónico con identidad Cliente para no
                    // perder el flujo (p. ej. cita del servicio ya elegido).
                    var guestResult = await DispatchGuestMessageAsync(
                        update,
                        messageText,
                        cancellationToken);
                    var linkedAfterGuest = await unitOfWork.UserLinksRepository
                        .GetByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);
                    if (linkedAfterGuest is not null &&
                        !string.IsNullOrWhiteSpace(guestResult.ResumeMessage))
                    {
                        var resumed = await ResumeAfterGuestLinkAsync(
                            update,
                            linkedAfterGuest,
                            guestResult.ResumeMessage!,
                            cancellationToken);
                        await DeliverAsync(
                            update,
                            CombineGuestAndResumed(guestResult.Message, resumed.Message),
                            cancellationToken);
                        return;
                    }

                    await DeliverAsync(update, ResponseText(guestResult), cancellationToken);
                    return;
                }

                await DeliverAsync(
                    update,
                    GuestAccessDisabledReply,
                    cancellationToken);
                return;
            }

            var idempotencyKey = $"telegram-update-{update.Id}-verified";
            var context = await ResolveConversationAsync(userLink, idempotencyKey, cancellationToken);

            // Ticket B3: se persiste cada mensaje de texto de un cliente vinculado,
            // sea o no una frase de escalamiento — la Recepcionista necesita ver el
            // mensaje que disparó el escalamiento, no solo la razón resumida que
            // queda en ChatEscalation.Reason. La respuesta del agente de IA queda
            // deliberadamente diferida (Decisión 2, Ticket B3): no hay todavía un
            // ChatParticipant "Agente IA" sembrado, y crear uno placeholder
            // solo para esto acoplaría este ticket a un sistema de métricas de costo
            // que nadie pidió todavía.
            await PersistClientMessageAsync(context.ConversationId, messageText, cancellationToken);

            if (context.IsEscalated)
            {
                await CompleteWithoutResponseAsync(update, cancellationToken);
                return;
            }

            if (IsEscalationRequest(messageText))
            {
                await EscalateAsync(context, messageText, cancellationToken);
                await DeliverAsync(update, EscalationConfirmedReply, cancellationToken);
                await PersistAssistantMessageAsync(
                    context.ConversationId,
                    userLink.ClientId,
                    EscalationConfirmedReply,
                    cancellationToken);
                return;
            }

            var result = await DispatchAuthenticatedAsync(
                context,
                userLink,
                messageText,
                idempotencyKey,
                update.Id,
                cancellationToken);
            await DeliverAsync(update, ResponseText(result), cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                await PersistAssistantMessageAsync(
                    context.ConversationId,
                    userLink.ClientId,
                    result.Message,
                    cancellationToken);
            }
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

    private async Task<AgentMessageResult> ResumeAfterGuestLinkAsync(
        TelegramInboundUpdate update,
        TelegramUserLink userLink,
        string resumeMessage,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"telegram-update-{update.Id}-resume";
        var context = await ResolveConversationAsync(userLink, idempotencyKey, cancellationToken);
        await PersistClientMessageAsync(context.ConversationId, resumeMessage, cancellationToken);
        if (context.IsEscalated)
        {
            return new AgentMessageResult(
                null,
                context.ConversationId,
                CreateCorrelationId(update.Id),
                "human_controlled",
                null,
                null,
                null,
                null,
                new AgentRagResult("skipped", "skipped", null, 0, 0, false, false),
                AgentAccessRequirement.None);
        }

        return await DispatchAuthenticatedAsync(
            context,
            userLink,
            resumeMessage,
            idempotencyKey,
            update.Id,
            cancellationToken);
    }

    private static string CombineGuestAndResumed(string? guestMessage, string? resumedMessage)
    {
        var guest = string.IsNullOrWhiteSpace(guestMessage) ? null : guestMessage.Trim();
        var resumed = string.IsNullOrWhiteSpace(resumedMessage) ? null : resumedMessage.Trim();
        if (guest is null)
        {
            return resumed ?? "Tu conversación está siendo atendida por un asesor.";
        }

        if (resumed is null)
        {
            return guest;
        }

        return $"{guest}\n\n{resumed}";
    }

    private async Task<AgentMessageResult> DispatchAuthenticatedAsync(
        AgentConversationContext context,
        TelegramUserLink userLink,
        string messageText,
        string idempotencyKey,
        long correlationSourceId,
        CancellationToken cancellationToken)
    {
        var identity = await identityProvider.GetAsync(userLink.ClientId, cancellationToken);
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

    // Crea el ChatEscalation para un cliente ya vinculado, a partir de la
    // ChatConversation ya resuelta por el llamador. Si ya hay un escalamiento
    // activo sin resolver (AgentConversationContext.IsEscalated, calculado por
    // el mismo IActiveConversationEscalationReader que usa
    // PersistentConversationContextProvider) no crea un segundo registro.
    private async Task EscalateAsync(
        AgentConversationContext context,
        string messageText,
        CancellationToken cancellationToken)
    {
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

    // Ticket B3: persiste el mensaje de texto del cliente en CHAT_MESSAGES.
    // El participante "Cliente" ya existe para toda ChatConversation resuelta
    // por PersistentConversationContextProvider — se busca por su tipo en vez
    // de crearlo de nuevo.
    private async Task PersistClientMessageAsync(
        Guid conversationId,
        string messageText,
        CancellationToken cancellationToken)
    {
        var participants = await sender.Send(
            new GetChatParticipantsByConversationIdQuery(conversationId),
            cancellationToken);
        var clientParticipant = participants.FirstOrDefault(
            participant => participant.ParticipantTypeId == conversationDefaults.ClientParticipantTypeId);
        if (clientParticipant is null)
        {
            logger.LogWarning(
                "No client ChatParticipant found for conversation {ConversationId}; skipping message persistence.",
                conversationId);
            return;
        }

        await sender.Send(
            new CreateChatMessageCommand(
                conversationId,
                clientParticipant.Id,
                conversationDefaults.ClientParticipantTypeId,
                messageText,
                Metadata: null),
            cancellationToken);
    }

    // Guarda la respuesta del asistente en CHAT_MESSAGES para que la bandeja de
    // asesores muestre la conversación completa. Se llama DESPUÉS de entregar el
    // mensaje: si el procesamiento se reintenta, no se duplica; y un fallo al
    // guardar nunca impide que el cliente reciba la respuesta.
    // CHAT_PARTICIPANTS exige exactamente una identidad (cliente o agente humano),
    // así que el participante "Agente IA" de la conversación se asocia al cliente
    // atendido; se distingue del participante "Cliente" por su tipo.
    private async Task PersistAssistantMessageAsync(
        Guid conversationId,
        Guid clientId,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            var participants = await sender.Send(
                new GetChatParticipantsByConversationIdQuery(conversationId),
                cancellationToken);
            var assistant = participants.FirstOrDefault(
                participant => participant.ParticipantTypeId == AiAgentSenderTypeId)
                ?? await sender.Send(
                    new CreateChatParticipantCommand(
                        conversationId,
                        AiAgentSenderTypeId,
                        clientId,
                        null),
                    cancellationToken);

            await sender.Send(
                new CreateChatMessageCommand(
                    conversationId,
                    assistant.Id,
                    AiAgentSenderTypeId,
                    text.Trim(),
                    Metadata: null),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "No se pudo guardar la respuesta del asistente en la conversación {ConversationId}.",
                conversationId);
        }
    }

    private static string ResponseText(AgentMessageResult result) =>
        string.IsNullOrWhiteSpace(result.Message)
            ? "Tu conversación está siendo atendida por un asesor."
            : result.Message;

    private async Task<AgentConversationContext> ResolveConversationAsync(
        TelegramUserLink userLink,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (userLink.ChatConversationId is { } conversationId)
        {
            var conversation = await sender.Send(
                new GetChatConversationByIdQuery(conversationId),
                cancellationToken);
            if (conversation is { Closed: false })
            {
                var lastActivity = conversation.LastMessageAt ?? conversation.CreatedAt;
                var idleFor = timeProvider.GetUtcNow().UtcDateTime - lastActivity;
                if (idleFor <= settings.PrivateAccessIdleLifetime)
                {
                    return await conversationContextProvider.ResolveAsync(
                        userLink.ClientId,
                        conversation.Id,
                        idempotencyKey,
                        "Telegram",
                        cancellationToken);
                }

                await sender.Send(
                    new CloseChatConversationCommand(conversation.Id),
                    cancellationToken);
                logger.LogInformation(
                    "Closed idle Telegram conversation {ConversationId} after {IdleMinutes} minutes.",
                    conversation.Id,
                    idleFor.TotalMinutes);
            }
        }

        AgentConversationContext? created = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            created = await conversationContextProvider.ResolveAsync(
                userLink.ClientId,
                null,
                idempotencyKey,
                "Telegram",
                transactionToken);
            var link = await unitOfWork.UserLinksRepository.GetByIdAsync(
                userLink.Id,
                transactionToken);
            if (link is not null)
            {
                link.BindConversation(created.ConversationId);
                await unitOfWork.UserLinksRepository.UpdateAsync(link, transactionToken);
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

    private async Task CompleteWithoutResponseAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken)
    {
        update.CompleteWithoutResponse(timeProvider.GetUtcNow().UtcDateTime);
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
