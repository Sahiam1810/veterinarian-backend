using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Results;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.Security.Registration;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using ClientEntity = Domain.Clients.Entities.ClientEntity;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Security.Authentication;

public sealed class ClientAccountRegistrationService(
    IUserAccountsRepository userAccounts,
    IUserCredentialsRepository userCredentials,
    IUsersRepository users,
    IClientRepository clients,
    IRolesRepository roles,
    IPasswordHasher passwordHasher,
    IOptions<SuperAdminOptions> superAdminOptions) : IClientAccountRegistrationService
{
    private const string ActiveStatus = "Activo";
    private const string ClientRoleName = "Cliente";
    private readonly SuperAdminOptions superAdmin = superAdminOptions.Value;

    public async Task<Result<RegisteredClientAccount>> StageAsync(
        ClientAccountRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.IdentificationNumber))
        {
            return Result<RegisteredClientAccount>.Failure(AuthenticationErrors.InvalidRegistrationData);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();
        var identificationNumber = request.IdentificationNumber.Trim();

        if (superAdmin.Enabled && string.Equals(
                normalizedEmail,
                superAdmin.Email.Trim().ToLowerInvariant(),
                StringComparison.Ordinal))
        {
            return Result<RegisteredClientAccount>.Failure(AuthenticationErrors.UserAlreadyExists);
        }

        var clientRole = await roles.GetByNameAsync(ClientRoleName, cancellationToken);
        if (clientRole is null)
        {
            return Result<RegisteredClientAccount>.Failure(AuthenticationErrors.InvalidRegistrationData);
        }

        if (await users.ExistsByEmailAsync(normalizedEmail, cancellationToken) ||
            await userAccounts.ExistsByUsernameAsync(normalizedUserName, cancellationToken) ||
            await userAccounts.GetByMailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result<RegisteredClientAccount>.Failure(AuthenticationErrors.UserAlreadyExists);
        }

        if (await clients.ExistsByIdentificationNumberAsync(identificationNumber, cancellationToken))
        {
            return Result<RegisteredClientAccount>.Failure(
                AuthenticationErrors.IdentificationNumberAlreadyExists);
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = new UserEntity(request.FullName.Trim(), normalizedEmail, passwordHash, clientRole.Id);
        var account = new UserAccountEntity(user.Id, normalizedUserName, normalizedEmail, ActiveStatus);
        var credential = new UserCredentialEntity(account.Id, passwordHash);
        var client = new ClientEntity(user.Id, identificationNumber, address: null);

        await users.AddAsync(user, cancellationToken);
        await userAccounts.AddAsync(account, cancellationToken);
        await userCredentials.AddAsync(credential, cancellationToken);
        await clients.AddAsync(client, cancellationToken);

        return Result<RegisteredClientAccount>.Success(new RegisteredClientAccount(
            user.Id,
            account.Id,
            clientRole.Id,
            clientRole.Name.Value,
            user.FullName,
            account.Username.Value,
            account.Mail.Value,
            account.Status));
    }
}
