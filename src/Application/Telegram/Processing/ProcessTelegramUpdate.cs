using System.Security.Cryptography;
using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
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
    ITelegramChatLinkingService linkingService,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider,
    ILogger<ProcessTelegramUpdateHandler> logger) : IRequestHandler<ProcessTelegramUpdateCommand>
{
    private const string LinkingRequiredReply =
        "¡Hola! Para proteger tu información, primero debes vincular este chat una sola vez. " +
        "Envía /vincular para comenzar.";
    private const string GuestStartReply =
        "¡Hola! Puedes hacer preguntas veterinarias generales como invitado. " +
        "Para consultar tus mascotas o realizar operaciones, envía /vincular; " +
        "si aún no tienes una cuenta, deberás crearla de forma segura en la aplicación.";

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

            var linkingOutcome = await linkingService.HandleAsync(update, cancellationToken);
            if (linkingOutcome.Consumed)
            {
                await DeliverAsync(
                    update,
                    linkingOutcome.Reply ?? "Solicitud procesada.",
                    cancellationToken);
                return;
            }

            var userLink = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
                update.TelegramUserId,
                cancellationToken);
            if (userLink is null)
            {
                if (settings.GuestModeEnabled &&
                    string.Equals(messageText, "/start", StringComparison.OrdinalIgnoreCase))
                {
                    await DeliverAsync(update, GuestStartReply, cancellationToken);
                    return;
                }

                if (settings.GuestModeEnabled)
                {
                    await ProcessGuestMessageAsync(update, messageText, cancellationToken);
                    return;
                }

                await DeliverAsync(
                    update,
                    LinkingRequiredReply,
                    cancellationToken);
                return;
            }

            var idempotencyKey = $"telegram-update-{update.Id}";
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
                    CreateCorrelationId(update.Id)),
                context with { Channel = "telegram" },
                identity.AccessToken,
                cancellationToken);
            var response = string.IsNullOrWhiteSpace(result.Message)
                ? "Tu conversación está siendo atendida por un asesor."
                : result.Message;
            await DeliverAsync(update, response, cancellationToken);
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

    private async Task ProcessGuestMessageAsync(
        TelegramInboundUpdate update,
        string messageText,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"telegram-update-{update.Id}";
        var identity = identityProvider.GetGuest(update.TelegramUserId);
        var context = new AgentConversationContext(
            CreateGuestConversationId(update.TelegramChatId),
            "telegram",
            false);
        var result = await dispatcher.DispatchAsync(
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
        var response = string.IsNullOrWhiteSpace(result.Message)
            ? GuestStartReply
            : result.Message;
        await DeliverAsync(update, response, cancellationToken);
    }

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
