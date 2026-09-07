using System.Net.Mail;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Verification.Abstractions;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Domain.Verification.Enums;

namespace Application.ContactVerification.UseCases;

// 3.1: reemplaza ContactEmailVerificationRequestStub con el envío real (hash, no OTP en claro).
public sealed class ContactEmailVerificationRequestHandler(
    IUnitOfWork unitOfWork,
    IContactVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IVerificationCodeDispatcher codeDispatcher,
    IContactVerificationSettings settings,
    TimeProvider timeProvider) : IRequestContactEmailVerification
{
    public async Task<RequestContactEmailVerificationResult> RequestAsync(
        RequestContactEmailVerification request,
        CancellationToken cancellationToken)
    {
        if (!MailAddress.TryCreate(request.Email, out var mailAddress))
        {
            throw new BadRequestException("El correo electrónico no tiene un formato válido.");
        }

        if (request.Purpose == ContactVerificationPurpose.Claim && request.SubjectUserId is null)
        {
            throw new BadRequestException("Claim exige el usuario sujeto.");
        }

        if (request.Purpose == ContactVerificationPurpose.Register && request.SubjectUserId is not null)
        {
            throw new BadRequestException("Register no admite usuario sujeto todavía.");
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
                throw new ConflictException(
                    "El código ya fue enviado. Espera un momento antes de solicitar otro.",
                    ContactVerificationErrors.ResendTooSoon.Code);
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
            throw new ConflictException(
                "No fue posible enviar el código en este momento. Intenta de nuevo.");
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

        return new RequestContactEmailVerificationResult(
            session.Id,
            expiresAt.UtcDateTime,
            ContactVerificationChannel.Email);
    }
}
