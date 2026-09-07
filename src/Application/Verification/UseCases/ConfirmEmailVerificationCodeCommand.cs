using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Application.Verification.Models;
using Domain.Verification.Enums;
using MediatR;

namespace Application.Verification.UseCases;

// Confirma el código OTP enviado por correo y genera el comprobante (EmailVerificationProof).
public sealed record ConfirmEmailVerificationCodeCommand(
    Guid SessionId,
    string Code) : IRequest<EmailVerificationProof>;

public sealed class ConfirmEmailVerificationCodeCommandHandler(
    IEmailVerificationSessionRepository sessions,
    IOtpProtector otpProtector,
    IEmailVerificationSettings settings,
    TimeProvider timeProvider,
    IUnitOfWork uow)
    : IRequestHandler<ConfirmEmailVerificationCodeCommand, EmailVerificationProof>
{
    public async Task<EmailVerificationProof> Handle(
        ConfirmEmailVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new NotFoundException("Sesión de verificación no encontrada.");

        var now = timeProvider.GetUtcNow();

        if (session.Status == VerificationSessionStatus.Blocked)
        {
            throw new BadRequestException("La sesión de verificación se encuentra bloqueada por exceder el número de intentos.");
        }

        if (session.Status != VerificationSessionStatus.AwaitingOtp)
        {
            throw new BadRequestException("La sesión de verificación no está activa.");
        }

        if (now.UtcDateTime >= session.ExpiresAt)
        {
            session.Expire(now.UtcDateTime);
            await sessions.UpdateAsync(session, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
            throw new BadRequestException("El código de verificación ha expirado.");
        }

        var isOtpValid = session.OtpHash is not null && otpProtector.Verify(request.Code, session.OtpHash);

        if (!isOtpValid)
        {
            session.RegisterFailedAttempt(settings.OtpMaximumAttempts, now.UtcDateTime);
            await sessions.UpdateAsync(session, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
            throw new BadRequestException("El código de verificación es incorrecto.");
        }

        var proofToken = session.Complete(now.UtcDateTime);
        await sessions.UpdateAsync(session, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var proofExpiresAt = now.AddHours(24).UtcDateTime;
        return new EmailVerificationProof(
            session.Email,
            proofToken,
            now.UtcDateTime,
            proofExpiresAt);
    }
}
