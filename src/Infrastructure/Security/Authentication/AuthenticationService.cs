using Application.Common.Abstractions;
using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Errors;
using Application.Security.Models;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.UserTokens.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Infrastructure.Security.Authentication;

public sealed class AuthenticationService(
    IUserAccountsRepository userAccountRepository,
    IUserCredentialsRepository userCredentialRepository,
    IUserTokensRepository userTokenRepository,
    IUsersRepository usersRepository,
    IUnitOfWork unitOfWork,
    JwtTokenIssuer jwtTokenIssuer,
    RefreshTokenProtector refreshTokenProtector,
    IPasswordHasher passwordHasher,
    IOptions<JwtOptions> options,
    IOptions<SuperAdminOptions> superAdminOptions,
    TimeProvider timeProvider) : IAuthenticationService
{
    private const string ActiveStatus = "Activo";
    private const string ClientRoleName = "Cliente";
    private const string RefreshTokenType = "refresh";

    private readonly JwtOptions jwtOptions = options.Value;
    private readonly SuperAdminOptions superAdmin = superAdminOptions.Value;

    public async Task<Result<AuthenticationTokens>> RegisterAsync(
        string fullName,
        string email,
        string userName,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRegistrationData);
        }

        var normalizedUserName = userName.Trim().ToLowerInvariant();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // El email del SuperAdmin no tiene fila en Users: si se permitiera
        // registrar aquí, quedaría una cuenta fantasma que nunca podría
        // loguearse (LoginAsync siempre intercepta ese email primero).
        if (IsSuperAdminEmail(normalizedEmail))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.UserAlreadyExists);
        }

        var clientRole = await unitOfWork.RolesRepository.GetByNameAsync(
            ClientRoleName, cancellationToken);

        if (clientRole is null)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRegistrationData);
        }

        if (await usersRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken) ||
            await userAccountRepository.ExistsByUsernameAsync(normalizedUserName, cancellationToken) ||
            await userAccountRepository.GetByMailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.UserAlreadyExists);
        }

        Result<AuthenticationTokens>? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var passwordHash = passwordHasher.Hash(password);

            var user = new UserEntity(
                fullName.Trim(),
                normalizedEmail,
                passwordHash,
                clientRole.Id);

            await usersRepository.AddAsync(user, transactionToken);

            var account = new UserAccountEntity(
                user.Id,
                normalizedUserName,
                normalizedEmail,
                ActiveStatus);

            await userAccountRepository.AddAsync(account, transactionToken);

            var credential = new UserCredentialEntity(
                account.Id,
                passwordHash);

            await userCredentialRepository.AddAsync(credential, transactionToken);

            var identity = new AuthenticatedIdentity(
                account.Id,
                user.Id,
                clientRole.Id,
                clientRole.Name.Value,
                user.FullName,
                account.Username.Value,
                account.Mail.Value,
                account.Status);

            result = await IssueTokensAsync(identity, transactionToken);
        }, cancellationToken);

        return result!;
    }

    public async Task<Result<AuthenticationTokens>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (IsSuperAdminEmail(normalizedEmail))
        {
            return passwordHasher.Verify(password, superAdmin.PasswordHash)
                ? IssueSuperAdminTokens()
                : Result<AuthenticationTokens>.Failure(AuthenticationErrors.InvalidCredentials);
        }

        var account = await userAccountRepository.GetByMailAsync(
            normalizedEmail, cancellationToken);

        if (!IsActiveAccount(account))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        var credential = await userCredentialRepository.GetByAccountIdAsync(
            account!.Id, cancellationToken);

        if (credential is null ||
            !passwordHasher.Verify(password, credential.PasswordHash))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        var identity = await BuildIdentityAsync(account, cancellationToken);

        if (identity is null)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        Result<AuthenticationTokens>? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            result = await IssueTokensAsync(identity, transactionToken);
        }, cancellationToken);

        return result!;
    }

    public async Task<Result<AuthenticationTokens>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenProtector.Hash(refreshToken);

        var currentToken = await userTokenRepository.GetByTokenValueAsync(
            tokenHash, cancellationToken);

        if (currentToken is null || currentToken.IsExpired)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        var account = await userAccountRepository.GetByIdAsync(
            currentToken.AccountId, cancellationToken);

        if (!IsActiveAccount(account))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        var identity = await BuildIdentityAsync(account!, cancellationToken);

        if (identity is null)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        Result<AuthenticationTokens>? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await userTokenRepository.DeleteAsync(currentToken, transactionToken);
            result = await IssueTokensAsync(identity, transactionToken);
        }, cancellationToken);

        return result!;
    }

    public async Task<Result> RevokeAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenProtector.Hash(refreshToken);

        var tokens = await userTokenRepository.GetAllByAccountIdAsync(
            userId,
            cancellationToken);

        var token = tokens.FirstOrDefault(candidate =>
            candidate.TokenValue == tokenHash);

        if (token is null)
        {
            return Result.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        await userTokenRepository.DeleteAsync(
            token,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CurrentProfile>> GetCurrentProfileAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        // El SuperAdmin no tiene fila en UserAccounts: sin este caso, /me le
        // devolvería 401 aunque su token sea válido.
        if (superAdmin.Enabled && userAccountId == superAdmin.Id)
        {
            return Result<CurrentProfile>.Success(BuildSuperAdminProfile());
        }

        var account = await userAccountRepository.GetByIdAsync(
            userAccountId, cancellationToken);

        if (!IsActiveAccount(account))
        {
            return Result<CurrentProfile>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        var identity = await BuildIdentityAsync(account!, cancellationToken);

        return identity is null
            ? Result<CurrentProfile>.Failure(AuthenticationErrors.InvalidCredentials)
            : Result<CurrentProfile>.Success(CurrentProfile.From(identity));
    }

    // El SuperAdmin no tiene UserAccounts, así que no hay dónde guardar un
    // refresh token: solo recibe access token, y vuelve a loguearse cuando expire.
    private Result<AuthenticationTokens> IssueSuperAdminTokens()
    {
        var accessToken = jwtTokenIssuer.IssueForSuperAdmin(superAdmin.Id, superAdmin.Email);

        return Result<AuthenticationTokens>.Success(
            new AuthenticationTokens(
                accessToken.Token,
                accessToken.ExpiresAt,
                string.Empty,
                accessToken.ExpiresAt));
    }

    private async Task<Result<AuthenticationTokens>> IssueTokensAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var rawRefreshToken = refreshTokenProtector.Generate();

        var refreshTokenHash = refreshTokenProtector.Hash(
            rawRefreshToken);

        var refreshTokenExpiresAt =
            now.AddDays(jwtOptions.RefreshTokenDays);

        var userToken = new UserTokenEntity(
            identity.UserAccountId,
            refreshTokenHash,
            RefreshTokenType,
            DateTime.SpecifyKind(
                refreshTokenExpiresAt.UtcDateTime,
                DateTimeKind.Unspecified));

        await userTokenRepository.AddAsync(
            userToken,
            cancellationToken);

        var accessToken = jwtTokenIssuer.Issue(
            identity);

        return Result<AuthenticationTokens>.Success(
            new AuthenticationTokens(
                accessToken.Token,
                accessToken.ExpiresAt,
                rawRefreshToken,
                refreshTokenExpiresAt));
    }

    private async Task<AuthenticatedIdentity?> BuildIdentityAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken)
    {
        var user = await usersRepository.GetByIdAsync(
            account.UserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var role = await unitOfWork.RolesRepository.GetByIdAsync(
            user.RoleId, cancellationToken);

        return new AuthenticatedIdentity(
            account.Id,
            user.Id,
            user.RoleId,
            role?.Name.Value ?? string.Empty,
            user.FullName,
            account.Username.Value,
            account.Mail.Value,
            account.Status);
    }

    private CurrentProfile BuildSuperAdminProfile() =>
        new(
            PersonId: superAdmin.Id,
            UserAccountId: superAdmin.Id,
            FullName: "Super Administrador",
            Initials: "SA",
            UserName: superAdmin.Email,
            Email: superAdmin.Email,
            Role: "SuperAdmin",
            AccountStatus: ActiveStatus);

    private bool IsSuperAdminEmail(string normalizedEmail) =>
        superAdmin.Enabled &&
        string.Equals(normalizedEmail, superAdmin.Email.Trim().ToLowerInvariant(), StringComparison.Ordinal);

    private static bool IsActiveAccount(UserAccountEntity? account) =>
        account is not null &&
        string.Equals(account.Status, ActiveStatus, StringComparison.Ordinal);
}
