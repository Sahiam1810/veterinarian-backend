using Application.Clients.Abstraction;
using Application.Roles.Abstraction;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using ClientEntity = Domain.Clients.Entities.ClientEntity;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Telegram.Identity;

public sealed class TelegramClientIdentityGateway(
    IClientRepository clients,
    IUsersRepository users,
    IUserAccountsRepository accounts,
    IRolesRepository roles) : ITelegramClientIdentityGateway
{
    private const string ActiveStatus = "Activo";
    private const string ClientRoleName = "Cliente";

    public async Task<TelegramClientIdentity?> FindActiveByIdentificationAsync(
        string identificationNumber,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationNumber);
        var client = await clients.GetByIdentificationNumberAsync(
            identificationNumber.Trim(),
            cancellationToken);
        return client is null
            ? null
            : await ResolveActiveAsync(client.UserId, cancellationToken);
    }

    public async Task<TelegramClientIdentity?> FindActiveByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        if (personId == Guid.Empty)
        {
            return null;
        }

        var client = await clients.GetByUserIdAsync(personId, cancellationToken);
        return client is null
            ? null
            : await ResolveActiveAsync(personId, cancellationToken);
    }

    public async Task<TelegramClientIdentity> StageRegistrationAsync(
        TelegramClientRegistration registration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var fullName = registration.FullName.Trim();
        var email = registration.Email.Trim().ToLowerInvariant();
        var identification = registration.IdentificationNumber.Trim();
        var role = await roles.GetByNameAsync(ClientRoleName, cancellationToken)
            ?? throw new TelegramAccountUnavailableException();

        if (await users.ExistsByEmailAsync(email, cancellationToken) ||
            await accounts.ExistsByMailAsync(email, cancellationToken) ||
            await clients.ExistsByIdentificationNumberAsync(identification, cancellationToken))
        {
            throw new TelegramIdentityConflictException();
        }

        string username;
        do
        {
            username = $"tg_{Guid.NewGuid():N}"[..30];
        }
        while (await accounts.ExistsByUsernameAsync(username, cancellationToken));

        var user = new UserEntity(fullName, email, passwordHash: null, role.Id);
        var account = new UserAccountEntity(user.Id, username, email, ActiveStatus);
        var client = new ClientEntity(user.Id, identification, address: null);
        await users.AddAsync(user, cancellationToken);
        await accounts.AddAsync(account, cancellationToken);
        await clients.AddAsync(client, cancellationToken);
        return new TelegramClientIdentity(user.Id, account.Id, account.Mail.Value);
    }

    private async Task<TelegramClientIdentity?> ResolveActiveAsync(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(personId, cancellationToken);
        if (user is not { IsActive: true })
        {
            return null;
        }

        var role = await roles.GetByIdAsync(user.RoleId, cancellationToken);
        if (role is null ||
            !string.Equals(role.Name.Value, ClientRoleName, StringComparison.Ordinal))
        {
            return null;
        }

        var account = await accounts.GetByUserIdAsync(personId, cancellationToken);
        if (account is null ||
            !string.Equals(account.Status, ActiveStatus, StringComparison.Ordinal))
        {
            return null;
        }

        return new TelegramClientIdentity(user.Id, account.Id, account.Mail.Value);
    }
}
