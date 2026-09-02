using System.Net.Http.Headers;
using System.Text;
using Application.Verification.Abstractions;
using Domain.Verification.Enums;
using Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging;

// Envío WhatsApp vía Twilio (mismo API Messages, From whatsapp:+...).
public sealed class TwilioWhatsAppVerificationCodeSender(
    IHttpClientFactory httpClientFactory,
    IOptions<TwilioOptions> options) : IVerificationCodeSender
{
    private readonly TwilioOptions _options = options.Value;

    public VerificationDeliveryChannel Channel => VerificationDeliveryChannel.WhatsApp;

    public async Task SendAsync(
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException(
                "El proveedor WhatsApp (Twilio) no está habilitado.");
        }

        if (string.IsNullOrWhiteSpace(_options.WhatsAppFrom))
        {
            throw new InvalidOperationException(
                "Twilio:WhatsAppFrom no está configurado.");
        }

        var digits = new string(destination.Where(char.IsDigit).ToArray());
        var to = digits.StartsWith(_options.DefaultCountryCode, StringComparison.Ordinal)
            ? $"whatsapp:+{digits}"
            : $"whatsapp:+{_options.DefaultCountryCode}{digits}";

        var body =
            $"Huellitas: tu código es {code}. Vence a las {expiresAt.UtcDateTime:HH:mm} UTC.";

        var client = httpClientFactory.CreateClient(nameof(TwilioWhatsAppVerificationCodeSender));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json");

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = to,
            ["From"] = _options.WhatsAppFrom,
            ["Body"] = body
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Twilio WhatsApp falló ({(int)response.StatusCode}): {detail}");
        }
    }
}
