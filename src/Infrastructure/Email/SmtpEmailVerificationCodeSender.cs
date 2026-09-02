using Application.Verification.Abstractions;
using Domain.Verification.Enums;
using Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Infrastructure.Email;

// Canal email del mecanismo genérico de verificación (antes SmtpTelegramVerificationCodeSender).
public sealed class SmtpEmailVerificationCodeSender(
    IOptions<EmailOptions> options,
    ISmtpTransport transport) : IVerificationCodeSender
{
    private readonly EmailOptions _options = options.Value;

    public VerificationDeliveryChannel Channel => VerificationDeliveryChannel.Email;

    public Task SendAsync(
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException(
                "El servicio de correo no está habilitado.");
        }

        var envelope = new SmtpEnvelope(
            destination,
            "Código de verificación de Huellitas",
            $"Tu código de verificación es: {code}{Environment.NewLine}{Environment.NewLine}" +
            $"Este código vence a las {expiresAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)}. " +
            "Si no solicitaste este código, ignora este mensaje.");
        return transport.SendAsync(envelope, cancellationToken);
    }
}
