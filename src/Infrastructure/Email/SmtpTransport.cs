using System.Net;
using System.Net.Mail;
using Infrastructure.Email.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public sealed class SmtpTransport(IOptions<EmailOptions> options) : ISmtpTransport
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(
        SmtpEnvelope envelope,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = envelope.Subject,
            Body = envelope.Body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(envelope.Destination));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseTls,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
