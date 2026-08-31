namespace Application.Telegram.Abstractions;

public sealed record TelegramProtectedCode(string RawCode, string Hash);

public interface ITelegramLinkCodeProtector
{
    TelegramProtectedCode Create();

    string Hash(string rawCode);
}
