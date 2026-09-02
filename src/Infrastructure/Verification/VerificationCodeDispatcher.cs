using Application.Verification.Abstractions;
using Domain.Verification.Enums;

namespace Infrastructure.Verification;

// Elige el IVerificationCodeSender registrado según el canal.
public sealed class VerificationCodeDispatcher(
    IEnumerable<IVerificationCodeSender> senders) : IVerificationCodeDispatcher
{
    private readonly Dictionary<VerificationDeliveryChannel, IVerificationCodeSender> _senders =
        senders.ToDictionary(s => s.Channel);

    public Task SendAsync(
        VerificationDeliveryChannel channel,
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (!_senders.TryGetValue(channel, out var sender))
        {
            throw new InvalidOperationException(
                $"No hay proveedor configurado para el canal {channel}.");
        }

        return sender.SendAsync(destination, code, expiresAt, cancellationToken);
    }
}
