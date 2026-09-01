using Application.Roles.Abstraction;
using Application.Security.Models;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Telegram.Security;

public sealed class AgentDelegatedIdentityProvider(
    IUsersRepository usersRepository,
    IUserAccountsRepository userAccountsRepository,
    IRolesRepository rolesRepository,
    ITelegramRuntimeSettings settings,
    JwtTokenIssuer tokenIssuer) : IAgentDelegatedIdentityProvider
{
    private const string GuestRole = "TelegramGuest";

    public AgentDelegatedIdentity GetGuest(long telegramUserId)
    {
        if (telegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));
        }

        var accountId = DeterministicId("account", telegramUserId);
        var personId = DeterministicId("person", telegramUserId);
        var roleId = DeterministicId("role", 1);
        var identity = new AuthenticatedIdentity(
            accountId,
            personId,
            roleId,
            GuestRole,
            "Telegram Guest",
            "telegram_guest",
            "guest@telegram.invalid",
            "Invitado");
        var token = tokenIssuer.Issue(identity, settings.DelegatedTokenLifetime);
        return new AgentDelegatedIdentity(personId, GuestRole, token.Token);
    }

    public async Task<AgentDelegatedIdentity> GetAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var user = await usersRepository.GetByIdAsync(personId, cancellationToken);
        var account = await userAccountsRepository.GetByUserIdAsync(personId, cancellationToken);
        if (user is null || !user.IsActive || account is null ||
            !string.Equals(account.Status, "Activo", StringComparison.Ordinal))
        {
            throw new TelegramAccountUnavailableException();
        }

        var role = await rolesRepository.GetByIdAsync(user.RoleId, cancellationToken);
        if (role is null || string.IsNullOrWhiteSpace(role.Name.Value))
        {
            throw new TelegramAccountUnavailableException();
        }

        var identity = new AuthenticatedIdentity(
            account.Id,
            user.Id,
            user.RoleId,
            role.Name.Value,
            user.FullName,
            account.Username.Value,
            account.Mail.Value,
            account.Status);
        var token = tokenIssuer.Issue(identity, settings.DelegatedTokenLifetime);
        return new AgentDelegatedIdentity(user.Id, role.Name.Value, token.Token);
    }

    private static Guid DeterministicId(string scope, long externalId)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"huellitas:telegram:guest:{scope}:{externalId}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
