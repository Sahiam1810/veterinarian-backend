using Domain.Verification.Enums;

namespace Application.Verification.Abstractions;

// Despacha el envío al proveedor registrado para el canal pedido.
public interface IVerificationCodeDispatcher
{
    Task SendAsync(
        VerificationDeliveryChannel channel,
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
