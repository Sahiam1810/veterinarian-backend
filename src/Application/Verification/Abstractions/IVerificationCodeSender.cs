using Domain.Verification.Enums;

namespace Application.Verification.Abstractions;

// Envío de código por un canal concreto (email, SMS, WhatsApp).
public interface IVerificationCodeSender
{
    VerificationDeliveryChannel Channel { get; }

    Task SendAsync(
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
