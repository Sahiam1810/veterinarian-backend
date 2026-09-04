using System.IdentityModel.Tokens.Jwt;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Domain.Roles;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Api.Tests.Security;

public sealed class AuthenticationServiceSuperAdminTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(
        2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    private const string SuperAdminEmail = "superadmin@huellitas.test";
    private const string SuperAdminPasswordHash = "stored-hash";

    private readonly IUserAccountsRepository accountRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository credentialRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUserTokensRepository tokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly JwtRsaKeyMaterial keyMaterial;
    private readonly AuthenticationService sut;

    public AuthenticationServiceSuperAdminTests()
    {
        var keys = RsaTestKeys.Create();
        var jwtOptions = new JwtOptions
        {
            Issuer = "Veterinaria.Api.Tests",
            Audience = "Veterinaria.Client.Tests",
            PrivateKeyPemBase64 = keys.PrivateKeyPemBase64,
            PublicKeyPemBase64 = keys.PublicKeyPemBase64,
            KeyId = "test-key-2026-08",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
            ClockSkewSeconds = 0
        };
        keyMaterial = new JwtRsaKeyMaterial(Options.Create(jwtOptions));
        var jwtTokenIssuer = new JwtTokenIssuer(
            Options.Create(jwtOptions), keyMaterial, new FixedTimeProvider(Now));

        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(CancellationToken.None));

        sut = new AuthenticationService(
            accountRepository,
            credentialRepository,
            tokenRepository,
            usersRepository,
            unitOfWork,
            jwtTokenIssuer,
            new RefreshTokenProtector(),
            passwordHasher,
            Options.Create(jwtOptions),
            new FixedTimeProvider(Now));
    }

    public void Dispose() => keyMaterial.Dispose();

    [Fact]
    public async Task LoginAsync_with_persisted_SuperAdmin_issues_normal_access_and_refresh_tokens()
    {
        var (user, account) = ConfigurePersistedSuperAdmin();
        passwordHasher.Verify("correct-password", SuperAdminPasswordHash).Returns(true);

        var result = await sut.LoginAsync(
            SuperAdminEmail.ToUpperInvariant(),
            "correct-password",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.RefreshToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal(account.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == "person_id").Value);
        Assert.Equal(SystemRoles.SuperAdminId.ToString(), jwt.Claims.Single(c => c.Type == "role_id").Value);
        Assert.Equal(SystemRoles.SuperAdminName, jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "super_admin");
        await tokenRepository.Received(1).AddAsync(
            Arg.Any<UserTokenEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_with_wrong_password_uses_the_persisted_account_and_fails()
    {
        ConfigurePersistedSuperAdmin();
        passwordHasher.Verify("wrong-password", SuperAdminPasswordHash).Returns(false);

        var result = await sut.LoginAsync(
            SuperAdminEmail,
            "wrong-password",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);
        await accountRepository.Received(1)
            .GetByMailAsync(SuperAdminEmail, Arg.Any<CancellationToken>());
        await tokenRepository.DidNotReceive().AddAsync(
            Arg.Any<UserTokenEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentProfileAsync_returns_the_persisted_SuperAdmin_profile()
    {
        var (user, account) = ConfigurePersistedSuperAdmin();
        accountRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var result = await sut.GetCurrentProfileAsync(account.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.PersonId);
        Assert.Equal(account.Id, result.Value.UserAccountId);
        Assert.Equal(SystemRoles.SuperAdminName, result.Value.Role);
        await accountRepository.Received(1)
            .GetByIdAsync(account.Id, Arg.Any<CancellationToken>());
    }

    private (UserEntity User, UserAccountEntity Account) ConfigurePersistedSuperAdmin()
    {
        var user = new UserEntity(
            "Super Administrador",
            SuperAdminEmail,
            SuperAdminPasswordHash,
            SystemRoles.SuperAdminId);
        var account = new UserAccountEntity(
            user.Id,
            "superadmin",
            SuperAdminEmail,
            "Activo");
        var credential = new UserCredentialEntity(account.Id, SuperAdminPasswordHash);
        var role = new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema");

        accountRepository.GetByMailAsync(SuperAdminEmail, Arg.Any<CancellationToken>()).Returns(account);
        credentialRepository.GetByAccountIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(credential);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>()).Returns(role);

        return (user, account);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
