using Application.Telegram.Models;

namespace Application.Telegram.Abstractions;

public interface ITelegramRegistrationAccountLookup
{
    Task<TelegramRegistrationAccount> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);
}
