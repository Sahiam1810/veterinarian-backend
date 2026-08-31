using Application.Telegram.Abstractions;
using Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Infrastructure.Email;

public sealed class SmtpTelegramVerificationCodeSender(
    IOptions<EmailOptions> options,
    ISmtpTransport transport) : ITelegramVerificationCodeSender
{
    private readonly EmailOptions _options = options.Value;

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
            "Si no solicitaste esta vinculación, ignora este mensaje.");
        return transport.SendAsync(envelope, cancellationToken);
    }
}
