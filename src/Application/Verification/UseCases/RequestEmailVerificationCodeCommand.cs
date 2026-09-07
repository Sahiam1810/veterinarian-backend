using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using MediatR;

namespace Application.Verification.UseCases;

// Solicita un código OTP enviado por correo electrónico.
public sealed record RequestEmailVerificationCodeCommand(string Email) : IRequest<Guid>;

public sealed class RequestEmailVerificationCodeCommandHandler(
    IEmailVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IVerificationCodeDispatcher codeDispatcher,
    IEmailVerificationSettings settings,
    TimeProvider timeProvider,
    IUnitOfWork uow)
    : IRequestHandler<RequestEmailVerificationCodeCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestEmailVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            throw new BadRequestException("El formato del correo electrónico no es válido.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow();
        var active = await sessions.GetActiveByEmailAsync(email, cancellationToken);

        if (active is not null)
        {
            var resendAllowedAt = active.UpdatedAt.GetValueOrDefault(active.CreatedAt)
                .Add(settings.OtpResendInterval);

            if (active.ExpiresAt > now.UtcDateTime && now.UtcDateTime < resendAllowedAt)
            {
                throw new ConflictException(
                    "El código ya fue enviado. Espera un momento antes de solicitar otro.");
            }

            active.Cancel(now.UtcDateTime);
            await sessions.UpdateAsync(active, cancellationToken);
        }

        var otp = otpProtector.Create();
        var destinationHash = otpProtector.HashPhone(email);
        var expiresAt = now.Add(settings.OtpLifetime);

        try
        {
            await codeDispatcher.SendAsync(
                VerificationDeliveryChannel.Email,
                email,
                otp.Code,
                expiresAt,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ConflictException(
                "No fue posible enviar el código de verificación por correo en este momento.");
        }

        var session = EmailVerificationSession.Start(
            email,
            destinationHash,
            otp.Hash,
            expiresAt.UtcDateTime,
            now.UtcDateTime);

        await sessions.AddAsync(session, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}
