using System.Security.Cryptography;
using System.Text;
using Infrastructure.Telegram.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Telegram.Security;

public interface ITelegramWebhookSecretValidator
{
    bool IsValid(string? candidate);
}

public sealed class TelegramWebhookSecretValidator(IOptions<TelegramOptions> options)
    : ITelegramWebhookSecretValidator
{
    private readonly byte[] expected = Encoding.UTF8.GetBytes(options.Value.WebhookSecret);

    public bool IsValid(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        var provided = Encoding.UTF8.GetBytes(candidate);
        return provided.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(provided, expected);
    }
}
