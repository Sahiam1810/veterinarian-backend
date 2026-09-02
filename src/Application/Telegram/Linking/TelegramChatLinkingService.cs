using Application.Telegram.Abstractions;
using Application.Verification.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Domain.Verification.Enums;

namespace Application.Telegram.Linking;

public sealed record TelegramLinkingOutcome(bool Consumed, string? Reply);

public interface ITelegramChatLinkingService
{
    Task<TelegramLinkingOutcome> HandleAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken);
}

// Vinculación Telegram usa el OTP genérico (canal Email).
public sealed class TelegramChatLinkingService(
    ITelegramUnitOfWork unitOfWork,
    ITelegramAccountLookup accountLookup,
    IVerificationCodeDispatcher verificationCodeDispatcher,
    IOtpProtector otpProtector,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider) : ITelegramChatLinkingService
{
    private const string GenericCodeSentReply =
        "Si el correo corresponde a una cuenta activa, recibirás un código de verificación. Escríbelo aquí.";

    public async Task<TelegramLinkingOutcome> HandleAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken)
    {
        var messageText = update.MessageText?.Trim();
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return new TelegramLinkingOutcome(false, null);
        }

        var now = timeProvider.GetUtcNow();
        if (string.Equals(messageText, "/cancelar", StringComparison.OrdinalIgnoreCase))
        {
            return await CancelAsync(update.TelegramUserId, now.UtcDateTime, cancellationToken);
        }

        if (messageText.StartsWith("/desvincular", StringComparison.OrdinalIgnoreCase))
        {
            return await UnlinkAsync(update.TelegramUserId, messageText, now.UtcDateTime, cancellationToken);
        }

        if (string.Equals(messageText, "/vincular", StringComparison.OrdinalIgnoreCase))
        {
            return await StartAsync(update, now.UtcDateTime, cancellationToken);
        }

        var session = await unitOfWork.LinkingSessionsRepository
            .GetActiveByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);
        if (session is null)
        {
            return new TelegramLinkingOutcome(false, null);
        }

        return session.Status switch
        {
            TelegramLinkingSessionStatus.AwaitingEmail => await ProcessEmailAsync(
                update,
                session,
                messageText,
                now,
                cancellationToken),
            TelegramLinkingSessionStatus.AwaitingOtp => await ProcessOtpAsync(
                update,
                session,
                messageText,
                now,
                cancellationToken),
            _ => new TelegramLinkingOutcome(false, null)
        };
    }

    private async Task<TelegramLinkingOutcome> StartAsync(
        TelegramInboundUpdate update,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var linked = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            update.TelegramUserId,
            cancellationToken);
        if (linked is not null)
        {
            return new TelegramLinkingOutcome(
                true,
                "Tu cuenta ya está vinculada. Puedes conversar conmigo normalmente.");
        }

        var active = await unitOfWork.LinkingSessionsRepository
            .GetActiveByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);
        if (active is not null)
        {
            if (active.Status == TelegramLinkingSessionStatus.AwaitingEmail)
            {
                return new TelegramLinkingOutcome(
                    true,
                    "Escribe el correo registrado en tu cuenta de Huellitas. Puedes usar /cancelar para salir.");
            }

            var resendAllowedAt = active.UpdatedAt.GetValueOrDefault(active.CreatedAt)
                .Add(settings.OtpResendInterval);
            if (active.Status == TelegramLinkingSessionStatus.AwaitingOtp &&
                active.ExpiresAt > now &&
                now < resendAllowedAt)
            {
                return new TelegramLinkingOutcome(
                    true,
                    "El código ya fue enviado. Espera un momento antes de solicitar otro.");
            }

            active.Cancel(now);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(active, cancellationToken);
        }

        var session = TelegramLinkingSession.Start(
            update.TelegramUserId,
            update.TelegramChatId,
            now);
        await unitOfWork.LinkingSessionsRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramLinkingOutcome(
            true,
            "Escribe el correo registrado en tu cuenta de Huellitas. Puedes usar /cancelar para salir.");
    }

    private async Task<TelegramLinkingOutcome> ProcessEmailAsync(
        TelegramInboundUpdate update,
        TelegramLinkingSession session,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);

        Application.Telegram.Models.TelegramLinkableAccount? account;
        try
        {
            account = await accountLookup.FindActiveByEmailAsync(email, cancellationToken);
        }
        catch (ArgumentException)
        {
            account = null;
        }

        var otp = otpProtector.Create();
        if (account is not null)
        {
            try
            {
                await verificationCodeDispatcher.SendAsync(
                    VerificationDeliveryChannel.Email,
                    account.Email,
                    otp.Code,
                    now.Add(settings.OtpLifetime),
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new TelegramLinkingOutcome(
                    true,
                    "No fue posible enviar el código en este momento. Intenta escribir tu correo nuevamente.");
            }
        }

        session.ResolveAccount(
            account?.PersonId,
            otpProtector.HashEmail(email),
            otp.Hash,
            now.Add(settings.OtpLifetime).UtcDateTime,
            now.UtcDateTime);
        await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramLinkingOutcome(true, GenericCodeSentReply);
    }

    private async Task<TelegramLinkingOutcome> ProcessOtpAsync(
        TelegramInboundUpdate update,
        TelegramLinkingSession session,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);
        if (session.ExpiresAt is null || now.UtcDateTime >= session.ExpiresAt)
        {
            session.Expire(now.UtcDateTime);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new TelegramLinkingOutcome(
                true,
                "El código venció. Envía /vincular para comenzar nuevamente.");
        }

        if (session.OtpHash is null ||
            session.PersonId is null ||
            !otpProtector.Verify(code, session.OtpHash))
        {
            session.RegisterFailedAttempt(settings.OtpMaximumAttempts, now.UtcDateTime);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var reply = session.Status == TelegramLinkingSessionStatus.Blocked
                ? "Se agotaron los intentos. Envía /vincular para comenzar nuevamente."
                : "El código no es válido. Verifícalo e intenta otra vez.";
            return new TelegramLinkingOutcome(true, reply);
        }

        var activeUserLink = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            update.TelegramUserId,
            cancellationToken);
        var activeChatLink = await unitOfWork.UserLinksRepository.GetByTelegramChatIdAsync(
            update.TelegramChatId,
            cancellationToken);
        if ((activeUserLink is not null && activeUserLink.PersonId != session.PersonId.Value) ||
            (activeChatLink is not null && activeChatLink.PersonId != session.PersonId.Value))
        {
            session.Cancel(now.UtcDateTime);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new TelegramLinkingOutcome(
                true,
                "Este chat ya está vinculado a otra cuenta de Huellitas.");
        }

        var personLink = await unitOfWork.UserLinksRepository.GetByPersonIdAsync(
            session.PersonId.Value,
            cancellationToken);
        if (personLink is { IsActive: true } &&
            personLink.TelegramUserId != update.TelegramUserId)
        {
            session.Cancel(now.UtcDateTime);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new TelegramLinkingOutcome(
                true,
                "La cuenta ya está vinculada a otro usuario de Telegram.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            if (personLink is null)
            {
                personLink = TelegramUserLink.Create(
                    session.PersonId.Value,
                    update.TelegramUserId,
                    update.TelegramChatId,
                    now.UtcDateTime);
                await unitOfWork.UserLinksRepository.AddAsync(personLink, transactionToken);
            }
            else
            {
                personLink.Relink(update.TelegramUserId, update.TelegramChatId, now.UtcDateTime);
                await unitOfWork.UserLinksRepository.UpdateAsync(personLink, transactionToken);
            }

            session.Complete(now.UtcDateTime);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, transactionToken);
        }, cancellationToken);
        return new TelegramLinkingOutcome(
            true,
            "Tu cuenta de Huellitas quedó vinculada correctamente.");
    }

    private async Task<TelegramLinkingOutcome> CancelAsync(
        long telegramUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var session = await unitOfWork.LinkingSessionsRepository
            .GetActiveByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (session is not null)
        {
            session.Cancel(now);
            await unitOfWork.LinkingSessionsRepository.UpdateAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new TelegramLinkingOutcome(true, "Proceso de vinculación cancelado.");
    }

    private async Task<TelegramLinkingOutcome> UnlinkAsync(
        long telegramUserId,
        string command,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command, "/desvincular confirmar", StringComparison.OrdinalIgnoreCase))
        {
            return new TelegramLinkingOutcome(
                true,
                "Para confirmar, envía /desvincular confirmar.");
        }

        var link = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            telegramUserId,
            cancellationToken);
        if (link is not null)
        {
            link.Revoke(now);
            await unitOfWork.UserLinksRepository.UpdateAsync(link, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new TelegramLinkingOutcome(true, "Tu cuenta de Huellitas quedó desvinculada.");
    }

    private async Task RedactAsync(
        TelegramInboundUpdate update,
        DateTime now,
        CancellationToken cancellationToken)
    {
        update.RedactSensitiveText(now);
        await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
