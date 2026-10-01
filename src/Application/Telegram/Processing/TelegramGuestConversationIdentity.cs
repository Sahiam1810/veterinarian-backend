using System.Security.Cryptography;
using System.Text;

namespace Application.Telegram.Processing;

public static class TelegramGuestConversationIdentity
{
    public static Guid GetConversationId(long telegramUserId)
    {
        if (telegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));
        }

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(
                $"huellitas:telegram:guest:conversation:{telegramUserId}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}