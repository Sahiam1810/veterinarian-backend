using Application.Telegram.Models;

namespace Application.Telegram.Abstractions;

public interface ITelegramAccountLookup
{
    Task<TelegramLinkableAccount?> FindActiveByEmailAsync(
        string email,
        CancellationToken cancellationToken);
}
