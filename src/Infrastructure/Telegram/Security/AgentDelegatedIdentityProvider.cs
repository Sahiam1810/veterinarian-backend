using Application.Roles.Abstraction;
using Application.Security.Models;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Tokens;

namespace Infrastructure.Telegram.Security;

public sealed class AgentDelegatedIdentityProvider(
    IUsersRepository usersRepository,
    IUserAccountsRepository userAccountsRepository,
    IRolesRepository rolesRepository,
    ITelegramRuntimeSettings settings,
    JwtTokenIssuer tokenIssuer) : IAgentDelegatedIdentityProvider
{
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
}
