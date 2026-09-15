using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.Abstractions;

// Solicitud de OTP de contacto por correo (3.1 implementa el puerto).
public sealed record RequestContactEmailVerification(
    string Email,
    ContactVerificationPurpose Purpose,
    Guid? SubjectUserId = null);

public sealed record RequestContactEmailVerificationResult(
    Guid SessionId,
    DateTime ExpiresAt,
    ContactVerificationChannel Channel);

public interface IRequestContactEmailVerification
{
    Task<RequestContactEmailVerificationResult> RequestAsync(
        RequestContactEmailVerification request,
        CancellationToken cancellationToken);
}
