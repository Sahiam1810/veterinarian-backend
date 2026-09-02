using System.Net.Http.Headers;
using System.Text;
using Application.Verification.Abstractions;
using Domain.Verification.Enums;
using Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging;

// Envío SMS vía Twilio REST API (canal del mecanismo genérico de verificación).
public sealed class TwilioSmsVerificationCodeSender(
    IHttpClientFactory httpClientFactory,
    IOptions<TwilioOptions> options) : IVerificationCodeSender
{
    private readonly TwilioOptions _options = options.Value;

    public VerificationDeliveryChannel Channel => VerificationDeliveryChannel.Sms;

    public async Task SendAsync(
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException(
                "El proveedor SMS (Twilio) no está habilitado.");
        }

        var to = FormatE164(destination);
        var body =
            $"Huellitas: tu código es {code}. Vence a las {expiresAt.UtcDateTime:HH:mm} UTC.";

        var client = httpClientFactory.CreateClient(nameof(TwilioSmsVerificationCodeSender));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json");

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = to,
            ["From"] = _options.FromNumber,
            ["Body"] = body
        });

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Twilio SMS falló ({(int)response.StatusCode}): {detail}");
        }
    }

    private string FormatE164(string destination)
    {
        var digits = new string(destination.Where(char.IsDigit).ToArray());
        if (digits.StartsWith(_options.DefaultCountryCode, StringComparison.Ordinal))
        {
            return $"+{digits}";
        }

        return $"+{_options.DefaultCountryCode}{digits}";
    }
}
