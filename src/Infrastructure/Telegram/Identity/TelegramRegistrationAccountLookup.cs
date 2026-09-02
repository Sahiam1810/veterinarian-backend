using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Domain.Telegram.Enums;

namespace Infrastructure.Telegram.Identity;

public sealed class TelegramRegistrationAccountLookup(
    IUserAccountsRepository accounts,
    IUsersRepository users) : ITelegramRegistrationAccountLookup
{
    private const string ActiveStatus = "Activo";

    public async Task<TelegramRegistrationAccount> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var email = normalizedEmail.Trim().ToLowerInvariant();
        var account = await accounts.GetByMailAsync(email, cancellationToken);
        if (account is null)
        {
            return new TelegramRegistrationAccount(
                TelegramRegistrationAccountKind.New, null, email);
        }

        var user = await users.GetByIdAsync(account.UserId, cancellationToken);
        var active = user is { IsActive: true } &&
            string.Equals(account.Status, ActiveStatus, StringComparison.Ordinal);
        return new TelegramRegistrationAccount(
            active ? TelegramRegistrationAccountKind.Active : TelegramRegistrationAccountKind.Inactive,
            user?.Id,
            account.Mail.Value);
    }
}
