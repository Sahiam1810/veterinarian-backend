using System.Net.Mail;
using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;

namespace Application.Telegram.Registration;

public sealed record TelegramRegistrationOutcome(bool Consumed, string? Reply);

public interface ITelegramRegistrationService
{
    Task<TelegramRegistrationOutcome> HandleAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken);
}

public sealed class TelegramRegistrationService(
    ITelegramUnitOfWork unitOfWork,
    ITelegramRegistrationAccountLookup accountLookup,
    ITelegramVerificationCodeSender verificationCodeSender,
    ITelegramOtpProtector otpProtector,
    ITelegramRegistrationProtector registrationProtector,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider) : ITelegramRegistrationService
{
    private const string CodeSentReply =
        "Si el correo es válido, recibirás un código de verificación. Escríbelo aquí.";

    public async Task<TelegramRegistrationOutcome> HandleAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken)
    {
        var text = update.MessageText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramRegistrationOutcome(false, null);
        }

        var now = timeProvider.GetUtcNow();
        var active = await unitOfWork.RegistrationSessionsRepository
            .GetActiveByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);

        if (string.Equals(text, "/cancelar", StringComparison.OrdinalIgnoreCase))
        {
            if (active is null)
            {
                return new TelegramRegistrationOutcome(false, null);
            }

            active.Cancel(now.UtcDateTime);
            await unitOfWork.RegistrationSessionsRepository.UpdateAsync(active, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new TelegramRegistrationOutcome(true, "Proceso de registro cancelado.");
        }

        if (string.Equals(text, "/registrar", StringComparison.OrdinalIgnoreCase))
        {
            return await StartAsync(update, active, now, cancellationToken);
        }

        if (active is null)
        {
            return new TelegramRegistrationOutcome(false, null);
        }

        return active.Status switch
        {
            TelegramRegistrationSessionStatus.AwaitingEmail => await ProcessEmailAsync(
                update, active, text, now, cancellationToken),
            TelegramRegistrationSessionStatus.AwaitingOtp => await ProcessOtpAsync(
                update, active, text, now, cancellationToken),
            TelegramRegistrationSessionStatus.AwaitingProfile => new TelegramRegistrationOutcome(
                true,
                "Ya envié el enlace de registro. Si venció, usa /cancelar y luego /registrar."),
            _ => new TelegramRegistrationOutcome(false, null)
        };
    }

    private async Task<TelegramRegistrationOutcome> StartAsync(
        TelegramInboundUpdate update,
        TelegramRegistrationSession? active,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!settings.RegistrationEnabled)
        {
            return new TelegramRegistrationOutcome(
                true, "El registro desde Telegram no está disponible en este momento.");
        }

        if (!string.Equals(update.ChatType, "private", StringComparison.Ordinal))
        {
            return new TelegramRegistrationOutcome(true, "El registro solo está disponible en chats privados.");
        }

        var linked = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            update.TelegramUserId, cancellationToken);
        if (linked is not null)
        {
            return new TelegramRegistrationOutcome(
                true, "Este chat ya está vinculado a una cuenta de Huellitas.");
        }

        if (active is not null)
        {
            return active.Status switch
            {
                TelegramRegistrationSessionStatus.AwaitingEmail => new TelegramRegistrationOutcome(
                    true, "Escribe el correo que deseas verificar. Puedes usar /cancelar para salir."),
                TelegramRegistrationSessionStatus.AwaitingOtp => new TelegramRegistrationOutcome(
                    true, "El código ya fue enviado. Escríbelo aquí o usa /cancelar."),
                _ => new TelegramRegistrationOutcome(
                    true, "Ya envié el enlace de registro. Si venció, usa /cancelar y luego /registrar.")
            };
        }

        var session = TelegramRegistrationSession.Start(
            update.TelegramUserId, update.TelegramChatId, now.UtcDateTime);
        await unitOfWork.RegistrationSessionsRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramRegistrationOutcome(
            true,
            "Escribe tu correo. Te enviaré un código para verificarlo. Puedes usar /cancelar para salir.");
    }

    private async Task<TelegramRegistrationOutcome> ProcessEmailAsync(
        TelegramInboundUpdate update,
        TelegramRegistrationSession session,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);
        if (!MailAddress.TryCreate(email, out var mailAddress))
        {
            return new TelegramRegistrationOutcome(true, "El correo no tiene un formato válido. Escríbelo nuevamente.");
        }

        var normalizedEmail = mailAddress.Address.Trim().ToLowerInvariant();
        var account = await accountLookup.FindByEmailAsync(normalizedEmail, cancellationToken);
        var otp = otpProtector.Create();
        try
        {
            await verificationCodeSender.SendAsync(
                account.NormalizedEmail,
                otp.Code,
                now.Add(settings.RegistrationOtpLifetime),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new TelegramRegistrationOutcome(
                true, "No fue posible enviar el código. Escribe tu correo para intentarlo nuevamente.");
        }

        session.PrepareOtp(
            registrationProtector.ProtectEmail(account.NormalizedEmail),
            otpProtector.HashEmail(account.NormalizedEmail),
            otp.Hash,
            account.Kind,
            account.PersonId,
            now.Add(settings.RegistrationOtpLifetime).UtcDateTime,
            now.UtcDateTime);
        await unitOfWork.RegistrationSessionsRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramRegistrationOutcome(true, CodeSentReply);
    }

    private async Task<TelegramRegistrationOutcome> ProcessOtpAsync(
        TelegramInboundUpdate update,
        TelegramRegistrationSession session,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);
        if (session.OtpExpiresAt is null || now.UtcDateTime >= session.OtpExpiresAt)
        {
            session.Expire(now.UtcDateTime);
            await SaveSessionAsync(session, cancellationToken);
            return new TelegramRegistrationOutcome(
                true, "El código venció. Envía /registrar para comenzar nuevamente.");
        }

        if (session.OtpHash is null || !otpProtector.Verify(code, session.OtpHash))
        {
            session.RegisterFailedOtp(settings.RegistrationMaximumOtpAttempts, now.UtcDateTime);
            await SaveSessionAsync(session, cancellationToken);
            return new TelegramRegistrationOutcome(
                true,
                session.Status == TelegramRegistrationSessionStatus.Blocked
                    ? "Se agotaron los intentos. Envía /registrar para comenzar nuevamente."
                    : "El código no es válido. Verifícalo e intenta otra vez.");
        }

        session.VerifyOtp(now.UtcDateTime);
        if (session.AccountKind == TelegramRegistrationAccountKind.Inactive)
        {
            session.Cancel(now.UtcDateTime);
            await SaveSessionAsync(session, cancellationToken);
            return new TelegramRegistrationOutcome(
                true, "La cuenta asociada está inactiva. Comunícate con soporte para recuperarla.");
        }

        if (session.AccountKind == TelegramRegistrationAccountKind.Active)
        {
            return await LinkExistingAsync(update, session, now.UtcDateTime, cancellationToken);
        }

        var token = registrationProtector.GenerateCompletionToken();
        session.IssueCompletionToken(
            registrationProtector.HashCompletionToken(token),
            now.Add(settings.RegistrationTokenLifetime).UtcDateTime,
            now.UtcDateTime);
        await SaveSessionAsync(session, cancellationToken);
        var separator = settings.RegistrationCompletionUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var url = $"{settings.RegistrationCompletionUrl}{separator}token={Uri.EscapeDataString(token)}";
        return new TelegramRegistrationOutcome(
            true, $"Correo verificado. Completa tu registro de forma segura aquí: {url}");
    }

    private async Task<TelegramRegistrationOutcome> LinkExistingAsync(
        TelegramInboundUpdate update,
        TelegramRegistrationSession session,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (session.PersonId is null)
        {
            session.Cancel(now);
            await SaveSessionAsync(session, cancellationToken);
            return new TelegramRegistrationOutcome(true, "No fue posible vincular la cuenta.");
        }

        var personLink = await unitOfWork.UserLinksRepository.GetByPersonIdAsync(
            session.PersonId.Value, cancellationToken);
        if (personLink is { IsActive: true } && personLink.TelegramUserId != update.TelegramUserId)
        {
            session.Cancel(now);
            await SaveSessionAsync(session, cancellationToken);
            return new TelegramRegistrationOutcome(
                true, "La cuenta ya está vinculada a otro usuario de Telegram.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            if (personLink is null)
            {
                personLink = TelegramUserLink.Create(
                    session.PersonId.Value, update.TelegramUserId, update.TelegramChatId, now);
                await unitOfWork.UserLinksRepository.AddAsync(personLink, transactionToken);
            }
            else
            {
                personLink.Relink(update.TelegramUserId, update.TelegramChatId, now);
                await unitOfWork.UserLinksRepository.UpdateAsync(personLink, transactionToken);
            }

            session.Complete(session.PersonId.Value, now);
            await unitOfWork.RegistrationSessionsRepository.UpdateAsync(session, transactionToken);
        }, cancellationToken);
        return new TelegramRegistrationOutcome(true, "Tu cuenta de Huellitas quedó vinculada correctamente.");
    }

    private async Task SaveSessionAsync(
        TelegramRegistrationSession session,
        CancellationToken cancellationToken)
    {
        await unitOfWork.RegistrationSessionsRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
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
