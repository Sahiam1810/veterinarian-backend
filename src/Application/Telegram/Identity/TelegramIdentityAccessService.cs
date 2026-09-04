using System.Net.Mail;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Models;
using Application.Verification.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Domain.Verification.Enums;

namespace Application.Telegram.Identity;

public interface ITelegramIdentityAccessService
{
    Task<TelegramIdentityAccessOutcome> BeginPrivateAccessAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken);

    Task<TelegramIdentityAccessOutcome> HandleActiveFlowAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken);

    Task<bool> HasValidAccessAsync(
        long telegramUserId,
        DateTime now,
        CancellationToken cancellationToken);

    Task TouchAsync(
        long telegramUserId,
        DateTime now,
        CancellationToken cancellationToken);
}

public sealed class TelegramIdentityAccessService(
    ITelegramUnitOfWork unitOfWork,
    ITelegramClientIdentityGateway clients,
    IVerificationCodeDispatcher verificationCodeDispatcher,
    IOtpProtector otpProtector,
    ITelegramIdentityDataProtector dataProtector,
    ITelegramRuntimeSettings settings,
    TimeProvider timeProvider) : ITelegramIdentityAccessService
{
    private const string IdentificationPurpose = "identification";
    private const string FullNamePurpose = "full-name";
    private const string EmailPurpose = "email";

    public async Task<TelegramIdentityAccessOutcome> BeginPrivateAccessAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await unitOfWork.IdentitySessionsRepository
            .GetCurrentByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);
        if (existing is not null &&
            existing.Status is not (
                TelegramIdentitySessionStatus.Cancelled or
                TelegramIdentitySessionStatus.Expired or
                TelegramIdentitySessionStatus.Blocked))
        {
            existing.Expire(now.UtcDateTime);
            await unitOfWork.IdentitySessionsRepository.UpdateAsync(existing, cancellationToken);
        }

        var session = TelegramIdentitySession.Start(
            update.TelegramUserId,
            update.TelegramChatId,
            update.Id,
            now.UtcDateTime);
        var link = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            update.TelegramUserId,
            cancellationToken);
        if (link is null)
        {
            await unitOfWork.IdentitySessionsRepository.AddAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new TelegramIdentityAccessOutcome(
                true,
                "Para proteger tus datos, escribe tu número de cédula. Puedes usar /cancelar para salir.");
        }

        var identity = await clients.FindActiveByPersonIdAsync(link.PersonId, cancellationToken);
        if (identity is null)
        {
            return new TelegramIdentityAccessOutcome(
                true,
                "Tu perfil de Huellitas no está disponible. Intenta nuevamente más tarde.");
        }

        var otp = otpProtector.Create();
        await SendOtpAsync(identity.Email, otp.Code, now, cancellationToken);
        session.BeginKnownClientOtp(
            identity.PersonId,
            otp.Hash,
            now.Add(settings.OtpLifetime).UtcDateTime,
            now.UtcDateTime);
        await unitOfWork.IdentitySessionsRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramIdentityAccessOutcome(
            true,
            "Enviamos un código de verificación a tu correo registrado. Escríbelo aquí.");
    }

    public async Task<TelegramIdentityAccessOutcome> HandleActiveFlowAsync(
        TelegramInboundUpdate update,
        CancellationToken cancellationToken)
    {
        var text = update.MessageText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramIdentityAccessOutcome(false, null);
        }

        var now = timeProvider.GetUtcNow();
        if (text.StartsWith("/desvincular", StringComparison.OrdinalIgnoreCase))
        {
            return await UnlinkAsync(update, text, now.UtcDateTime, cancellationToken);
        }

        var session = await unitOfWork.IdentitySessionsRepository
            .GetCurrentByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);
        if (session is null || session.Status == TelegramIdentitySessionStatus.Verified)
        {
            if (string.Equals(text, "/vincular", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "/registrar", StringComparison.OrdinalIgnoreCase))
            {
                return new TelegramIdentityAccessOutcome(
                    true,
                    "Puedes conversar normalmente. Solo pediré verificar tu identidad cuando solicites información privada.");
            }

            return new TelegramIdentityAccessOutcome(false, null);
        }

        if (string.Equals(text, "/cancelar", StringComparison.OrdinalIgnoreCase))
        {
            if (session.Status is not TelegramIdentitySessionStatus.Cancelled and
                not TelegramIdentitySessionStatus.Expired and
                not TelegramIdentitySessionStatus.Blocked)
            {
                session.Cancel(now.UtcDateTime);
                await unitOfWork.IdentitySessionsRepository.UpdateAsync(session, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new TelegramIdentityAccessOutcome(true, "Proceso de verificación cancelado.");
        }

        return session.Status switch
        {
            TelegramIdentitySessionStatus.AwaitingIdentification =>
                await ProcessIdentificationAsync(update, session, text, now, cancellationToken),
            TelegramIdentitySessionStatus.AwaitingRegistrationConfirmation =>
                await ProcessRegistrationConfirmationAsync(session, text, now.UtcDateTime, cancellationToken),
            TelegramIdentitySessionStatus.AwaitingFullName =>
                await ProcessFullNameAsync(update, session, text, now.UtcDateTime, cancellationToken),
            TelegramIdentitySessionStatus.AwaitingEmail =>
                await ProcessEmailAsync(update, session, text, now, cancellationToken),
            TelegramIdentitySessionStatus.AwaitingOtp =>
                await ProcessOtpAsync(update, session, text, now, cancellationToken),
            _ => new TelegramIdentityAccessOutcome(false, null)
        };
    }

    public async Task<bool> HasValidAccessAsync(
        long telegramUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var session = await unitOfWork.IdentitySessionsRepository
            .GetCurrentByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (session is null || session.Status != TelegramIdentitySessionStatus.Verified)
        {
            return false;
        }

        if (session.IsAccessValid(now))
        {
            return true;
        }

        session.Expire(now);
        await unitOfWork.IdentitySessionsRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return false;
    }

    public async Task TouchAsync(
        long telegramUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var session = await unitOfWork.IdentitySessionsRepository
            .GetCurrentByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (session is null || !session.IsAccessValid(now))
        {
            return;
        }

        session.Touch(now.Add(settings.PrivateAccessIdleLifetime), now);
        await unitOfWork.IdentitySessionsRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TelegramIdentityAccessOutcome> ProcessIdentificationAsync(
        TelegramInboundUpdate update,
        TelegramIdentitySession session,
        string identification,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);
        var identity = await clients.FindActiveByIdentificationAsync(
            identification,
            cancellationToken);
        if (identity is null)
        {
            session.RequireRegistration(
                dataProtector.Protect(IdentificationPurpose, identification),
                now.UtcDateTime);
            await PersistSessionAsync(session, cancellationToken);
            return new TelegramIdentityAccessOutcome(
                true,
                "No encontramos un perfil con esa cédula. Responde sí para registrarte como cliente o usa /cancelar.");
        }

        var otp = otpProtector.Create();
        await SendOtpAsync(identity.Email, otp.Code, now, cancellationToken);
        session.BeginKnownClientOtp(
            identity.PersonId,
            otp.Hash,
            now.Add(settings.OtpLifetime).UtcDateTime,
            now.UtcDateTime);
        await PersistSessionAsync(session, cancellationToken);
        return new TelegramIdentityAccessOutcome(
            true,
            "Enviamos un código de verificación a tu correo registrado. Escríbelo aquí.");
    }

    private async Task<TelegramIdentityAccessOutcome> ProcessRegistrationConfirmationAsync(
        TelegramIdentitySession session,
        string text,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!IsAffirmative(text))
        {
            session.Cancel(now);
            await PersistSessionAsync(session, cancellationToken);
            return new TelegramIdentityAccessOutcome(true, "Registro cancelado.");
        }

        session.ConfirmRegistration(now);
        await PersistSessionAsync(session, cancellationToken);
        return new TelegramIdentityAccessOutcome(true, "Escribe tu nombre completo.");
    }

    private async Task<TelegramIdentityAccessOutcome> ProcessFullNameAsync(
        TelegramInboundUpdate update,
        TelegramIdentitySession session,
        string fullName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now, cancellationToken);
        if (fullName.Length is < 3 or > 100)
        {
            return new TelegramIdentityAccessOutcome(true, "Escribe un nombre completo válido.");
        }

        session.CaptureFullName(dataProtector.Protect(FullNamePurpose, fullName), now);
        await PersistSessionAsync(session, cancellationToken);
        return new TelegramIdentityAccessOutcome(true, "Escribe tu correo electrónico.");
    }

    private async Task<TelegramIdentityAccessOutcome> ProcessEmailAsync(
        TelegramInboundUpdate update,
        TelegramIdentitySession session,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalizedEmail, out var address) ||
            !string.Equals(address.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return new TelegramIdentityAccessOutcome(true, "Escribe un correo electrónico válido.");
        }

        var otp = otpProtector.Create();
        await SendOtpAsync(normalizedEmail, otp.Code, now, cancellationToken);
        session.BeginRegistrationOtp(
            dataProtector.Protect(EmailPurpose, normalizedEmail),
            otp.Hash,
            now.Add(settings.OtpLifetime).UtcDateTime,
            now.UtcDateTime);
        await PersistSessionAsync(session, cancellationToken);
        return new TelegramIdentityAccessOutcome(
            true,
            "Enviamos un código de verificación al correo indicado. Escríbelo aquí.");
    }

    private async Task<TelegramIdentityAccessOutcome> ProcessOtpAsync(
        TelegramInboundUpdate update,
        TelegramIdentitySession session,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await RedactAsync(update, now.UtcDateTime, cancellationToken);
        if (session.OtpExpiresAt is null || now.UtcDateTime >= session.OtpExpiresAt)
        {
            session.Expire(now.UtcDateTime);
            await PersistSessionAsync(session, cancellationToken);
            return new TelegramIdentityAccessOutcome(
                true,
                "El código venció. Repite tu solicitud privada para recibir uno nuevo.");
        }

        if (session.OtpHash is null || !otpProtector.Verify(code, session.OtpHash))
        {
            session.RegisterFailedOtpAttempt(settings.OtpMaximumAttempts, now.UtcDateTime);
            await PersistSessionAsync(session, cancellationToken);
            var reply = session.Status == TelegramIdentitySessionStatus.Blocked
                ? "Se agotaron los intentos. Repite tu solicitud privada para iniciar nuevamente."
                : "El código no es válido. Verifícalo e intenta otra vez.";
            return new TelegramIdentityAccessOutcome(true, reply);
        }

        Guid? personId = null;
        long? pendingUpdateId = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var identity = session.PersonId is not null
                ? new TelegramClientIdentity(session.PersonId.Value, Guid.Empty, string.Empty)
                : await clients.StageRegistrationAsync(
                    new TelegramClientRegistration(
                        dataProtector.Unprotect(
                            IdentificationPurpose,
                            session.ProtectedIdentification!),
                        dataProtector.Unprotect(FullNamePurpose, session.ProtectedFullName!),
                        dataProtector.Unprotect(EmailPurpose, session.ProtectedEmail!)),
                    transactionToken);
            await EnsureUserLinkAsync(session, identity.PersonId, now.UtcDateTime, transactionToken);
            session.Verify(
                identity.PersonId,
                now.Add(settings.PrivateAccessAbsoluteLifetime).UtcDateTime,
                now.Add(settings.PrivateAccessIdleLifetime).UtcDateTime,
                now.UtcDateTime);
            pendingUpdateId = session.TakePendingInboundUpdate(now.UtcDateTime);
            personId = identity.PersonId;
            await unitOfWork.IdentitySessionsRepository.UpdateAsync(session, transactionToken);
        }, cancellationToken);

        return new TelegramIdentityAccessOutcome(
            true,
            "Identidad verificada correctamente.",
            personId,
            pendingUpdateId);
    }

    private async Task EnsureUserLinkAsync(
        TelegramIdentitySession session,
        Guid personId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var telegramLink = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            session.TelegramUserId,
            cancellationToken);
        if (telegramLink is not null && telegramLink.PersonId != personId)
        {
            throw new TelegramIdentityConflictException();
        }

        var chatLink = await unitOfWork.UserLinksRepository.GetByTelegramChatIdAsync(
            session.TelegramChatId,
            cancellationToken);
        if (chatLink is not null && chatLink.PersonId != personId)
        {
            throw new TelegramIdentityConflictException();
        }

        var personLink = await unitOfWork.UserLinksRepository.GetByPersonIdAsync(
            personId,
            cancellationToken);
        if (personLink is null)
        {
            personLink = TelegramUserLink.Create(
                personId,
                session.TelegramUserId,
                session.TelegramChatId,
                now);
            await unitOfWork.UserLinksRepository.AddAsync(personLink, cancellationToken);
            return;
        }

        if (personLink.IsActive && personLink.TelegramUserId != session.TelegramUserId)
        {
            throw new TelegramIdentityConflictException();
        }

        if (!personLink.IsActive)
        {
            personLink.Relink(session.TelegramUserId, session.TelegramChatId, now);
            await unitOfWork.UserLinksRepository.UpdateAsync(personLink, cancellationToken);
        }
    }

    private async Task<TelegramIdentityAccessOutcome> UnlinkAsync(
        TelegramInboundUpdate update,
        string command,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command, "/desvincular confirmar", StringComparison.OrdinalIgnoreCase))
        {
            return new TelegramIdentityAccessOutcome(
                true,
                "Para confirmar, envía /desvincular confirmar.");
        }

        var link = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
            update.TelegramUserId,
            cancellationToken);
        if (link is not null)
        {
            link.Revoke(now);
            await unitOfWork.UserLinksRepository.UpdateAsync(link, cancellationToken);
        }

        var session = await unitOfWork.IdentitySessionsRepository
            .GetCurrentByTelegramUserIdAsync(update.TelegramUserId, cancellationToken);
        if (session is not null &&
            session.Status is not (
                TelegramIdentitySessionStatus.Cancelled or
                TelegramIdentitySessionStatus.Expired or
                TelegramIdentitySessionStatus.Blocked))
        {
            session.Cancel(now);
            await unitOfWork.IdentitySessionsRepository.UpdateAsync(session, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TelegramIdentityAccessOutcome(true, "Tu acceso de Telegram quedó desvinculado.");
    }

    private Task SendOtpAsync(
        string email,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        verificationCodeDispatcher.SendAsync(
            VerificationDeliveryChannel.Email,
            email,
            code,
            now.Add(settings.OtpLifetime),
            cancellationToken);

    private async Task RedactAsync(
        TelegramInboundUpdate update,
        DateTime now,
        CancellationToken cancellationToken)
    {
        update.RedactSensitiveText(now);
        await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistSessionAsync(
        TelegramIdentitySession session,
        CancellationToken cancellationToken)
    {
        await unitOfWork.IdentitySessionsRepository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool IsAffirmative(string text) =>
        string.Equals(text, "sí", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(text, "si", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(text, "confirmar", StringComparison.OrdinalIgnoreCase);
}
