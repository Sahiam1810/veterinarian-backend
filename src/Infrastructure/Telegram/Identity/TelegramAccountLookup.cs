using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;

namespace Infrastructure.Telegram.Identity;

public sealed class TelegramAccountLookup(
    IUserAccountsRepository accountsRepository,
    IUsersRepository usersRepository) : ITelegramAccountLookup
{
    private const string ActiveStatus = "Activo";

    public async Task<TelegramLinkableAccount?> FindActiveByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var account = await accountsRepository.GetByMailAsync(
            normalizedEmail,
            cancellationToken);
        if (account is null ||
            !string.Equals(account.Status, ActiveStatus, StringComparison.Ordinal))
        {
            return null;
        }

        var user = await usersRepository.GetByIdAsync(
            account.UserId,
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return new TelegramLinkableAccount(user.Id, account.Mail.Value);
    }
}
