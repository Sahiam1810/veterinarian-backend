using System.Security.Cryptography;
using System.Text;
using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Security;

public sealed class TelegramLinkCodeProtector : ITelegramLinkCodeProtector
{
    public TelegramProtectedCode Create()
    {
        var rawCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return new TelegramProtectedCode(rawCode, Hash(rawCode));
    }

    public string Hash(string rawCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCode);
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawCode)));
    }
}
