using System.Net.Mail;
using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Verification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Domain.Verification.Enums;
using Microsoft.Extensions.Logging;

namespace Application.ContactVerification.UseCases;

// 3.1: solicitud OTP de contacto por Email (hash + SMTP vía dispatcher; nunca OTP en response/logs).
public sealed class ContactEmailVerificationRequestHandler(
    IUnitOfWork unitOfWork,
    IContactVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IVerificationCodeDispatcher codeDispatcher,
    IContactVerificationSettings settings,
    TimeProvider timeProvider,
    ILogger<ContactEmailVerificationRequestHandler> logger) : IRequestContactEmailVerification
{
    public async Task<RequestContactEmailVerificationResult> RequestAsync(
        RequestContactEmailVerification request,
        CancellationToken cancellationToken)
    {
        if (!MailAddress.TryCreate(request.Email, out var mailAddress))
        {
            throw new ContactVerificationException(ContactVerificationErrors.EmailInvalid);
        }

        if (request.Purpose == ContactVerificationPurpose.Claim && request.SubjectUserId is null)
        {
            throw new ContactVerificationException(ContactVerificationErrors.PurposeInvalid);
        }

        if (request.Purpose == ContactVerificationPurpose.Register && request.SubjectUserId is not null)
        {
            throw new ContactVerificationException(ContactVerificationErrors.PurposeInvalid);
        }

        var normalizedEmail = mailAddress.Address.Trim().ToLowerInvariant();
        var destinationHash = otpProtector.HashEmail(normalizedEmail);
        var now = timeProvider.GetUtcNow();

        var active = await sessions.GetActiveByPurposeAndDestinationAsync(
            request.Purpose,
            destinationHash,
            cancellationToken);

        if (active is not null)
        {
            var resendAllowedAt = active.UpdatedAt.GetValueOrDefault(active.CreatedAt)
                .Add(settings.OtpResendInterval);
            if (active.ExpiresAt > now.UtcDateTime && now.UtcDateTime < resendAllowedAt)
            {
                throw new ContactVerificationException(ContactVerificationErrors.ResendTooSoon);
            }

            active.Cancel(now.UtcDateTime);
            await sessions.UpdateAsync(active, cancellationToken);
        }

        var otp = otpProtector.Create();
        var expiresAt = now.Add(settings.OtpLifetime);

        try
        {
            await codeDispatcher.SendAsync(
                VerificationDeliveryChannel.Email,
                normalizedEmail,
                otp.Code,
                expiresAt,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Fallo al enviar OTP de contacto. Purpose={Purpose}",
                request.Purpose);
            throw new ContactVerificationException(ContactVerificationErrors.DeliveryFailed);
        }

        var session = ContactVerificationSession.Start(
            request.Purpose,
            ContactVerificationChannel.Email,
            destinationHash,
            otp.Hash,
            expiresAt.UtcDateTime,
            now.UtcDateTime,
            request.SubjectUserId);

        await sessions.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Metadatos seguros: sessionId + purpose; nunca el OTP ni el correo en claro.
        logger.LogInformation(
            "OTP de contacto solicitado. SessionId={SessionId} Purpose={Purpose} Channel={Channel}",
            session.Id,
            session.Purpose,
            session.Channel);

        return new RequestContactEmailVerificationResult(
            session.Id,
            expiresAt.UtcDateTime,
            ContactVerificationChannel.Email);
    }
}
