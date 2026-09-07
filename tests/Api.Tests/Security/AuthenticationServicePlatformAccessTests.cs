using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Permissions.Claims;
using Application.Permissions.UseCases;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.Security.Authentication;
using RoleEntity = Domain.Roles.Entities.Roles;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using MediatR;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Security;

public sealed class AuthenticationServicePlatformAccessTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid StaffRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ClientRoleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string Password = "CorrectPassword1!";
    private const string PasswordHash = "stored-hash";

    private readonly IUserAccountsRepository userAccountRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository userCredentialRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUserTokensRepository userTokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly JwtRsaKeyMaterial keyMaterial;
    private readonly AuthenticationService sut;

    public AuthenticationServicePlatformAccessTests()
    {
        var keys = RsaTestKeys.Create();
        var jwtOptions = new JwtOptions
        {
            Issuer = "Veterinaria.Api.Tests",
            Audience = "Veterinaria.Client.Tests",
            PrivateKeyPemBase64 = keys.PrivateKeyPemBase64,
            PublicKeyPemBase64 = keys.PublicKeyPemBase64,
            KeyId = "platform-access-test-key",
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
        sender.Send(
                Arg.Any<GetUserPermissionClaimsQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(["perm:Mascotas:View"]);

        sut = new AuthenticationService(
            userAccountRepository,
            userCredentialRepository,
            userTokenRepository,
            usersRepository,
            unitOfWork,
            sender,
            jwtTokenIssuer,
            new RefreshTokenProtector(),
            passwordHasher,
            Options.Create(jwtOptions),
            new FixedTimeProvider(Now));
    }

    public void Dispose() => keyMaterial.Dispose();

    [Fact]
    public async Task LoginAsync_unknown_email_returns_InvalidCredentials_without_tokens()
    {
        userAccountRepository
            .GetByMailAsync("ghost@huellitas.test", Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        var result = await sut.LoginAsync(
            "ghost@huellitas.test", Password, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);
        await userCredentialRepository.DidNotReceive()
            .GetByAccountIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_staff_wrong_password_returns_InvalidCredentials_same_as_unknown_email()
    {
        var fixture = ArrangeStaffAccount(active: true);
        passwordHasher.Verify(Password, PasswordHash).Returns(false);

        var result = await sut.LoginAsync(fixture.Email, Password, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);
        Assert.Equal(
            AuthenticationErrors.InvalidCredentials.Code,
            result.Error.Code);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_client_with_valid_legacy_password_returns_PlatformAccessDenied_without_tokens()
    {
        var fixture = ArrangeClientAccount(active: true);
        passwordHasher.Verify(Password, PasswordHash).Returns(true);

        var result = await sut.LoginAsync(fixture.Email, Password, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied, result.Error);
        Assert.DoesNotContain("Cliente", result.Error.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.Email, result.Error.Description, StringComparison.OrdinalIgnoreCase);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_inactive_staff_with_valid_password_returns_PlatformAccessDenied_without_tokens()
    {
        var fixture = ArrangeStaffAccount(active: false);
        passwordHasher.Verify(Password, PasswordHash).Returns(true);

        var result = await sut.LoginAsync(fixture.Email, Password, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied, result.Error);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_active_staff_with_valid_password_issues_tokens()
    {
        var fixture = ArrangeStaffAccount(active: true);
        passwordHasher.Verify(Password, PasswordHash).Returns(true);

        var result = await sut.LoginAsync(fixture.Email, Password, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(result.Value.AccessToken);
        Assert.Contains(
            token.Claims,
            claim => claim.Type == PermissionClaimValue.ClaimType &&
                     claim.Value == "perm:Mascotas:View");
        await userTokenRepository.Received(1)
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_inactive_staff_with_wrong_password_returns_InvalidCredentials_not_PlatformAccessDenied()
    {
        var fixture = ArrangeStaffAccount(active: false);
        passwordHasher.Verify(Password, PasswordHash).Returns(false);

        var result = await sut.LoginAsync(fixture.Email, Password, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task RefreshAsync_client_with_valid_token_returns_PlatformAccessDenied_without_tokens()
    {
        var fixture = ArrangeClientAccount(active: true);
        ArrangeValidRefreshToken(fixture.AccountId);

        var result = await sut.RefreshAsync("raw-refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied, result.Error);
        Assert.DoesNotContain("Cliente", result.Error.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.Email, result.Error.Description, StringComparison.OrdinalIgnoreCase);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
        await userTokenRepository.DidNotReceive()
            .DeleteAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_inactive_staff_with_valid_token_returns_PlatformAccessDenied_without_tokens()
    {
        var fixture = ArrangeStaffAccount(active: false);
        ArrangeValidRefreshToken(fixture.AccountId);

        var result = await sut.RefreshAsync("raw-refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied, result.Error);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_active_staff_with_valid_token_issues_tokens()
    {
        var fixture = ArrangeStaffAccount(active: true);
        ArrangeValidRefreshToken(fixture.AccountId);

        var result = await sut.RefreshAsync("raw-refresh-token", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(result.Value.AccessToken);
        Assert.Contains(
            token.Claims,
            claim => claim.Type == PermissionClaimValue.ClaimType &&
                     claim.Value == "perm:Mascotas:View");
        await userTokenRepository.Received(1)
            .AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_invalid_token_returns_InvalidRefreshToken()
    {
        userTokenRepository
            .GetByTokenValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Domain.UserTokens.Entities.UserTokens?)null);

        var result = await sut.RefreshAsync("bogus-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRefreshToken, result.Error);
        await userAccountRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeValidRefreshToken(Guid accountId)
    {
        var protector = new RefreshTokenProtector();
        var hash = protector.Hash("raw-refresh-token");
        var token = new Domain.UserTokens.Entities.UserTokens(
            accountId,
            hash,
            "refresh",
            Now.AddDays(7).UtcDateTime);

        userTokenRepository
            .GetByTokenValueAsync(hash, Arg.Any<CancellationToken>())
            .Returns(token);
    }

    private AccountFixture ArrangeStaffAccount(bool active)
    {
        var email = "staff@huellitas.test";
        var user = new UserEntity("Staff User", email, PasswordHash, StaffRoleId);
        var account = new UserAccountEntity(
            user.Id,
            "staff",
            email,
            active ? "Activo" : "Inactivo");
        var credentials = new UserCredentialsEntity(account.Id, PasswordHash);
        var role = new RoleEntity("Administrador", "Staff panel");

        userAccountRepository.GetByMailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        userAccountRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        userCredentialRepository.GetByAccountIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(credentials);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(StaffRoleId, Arg.Any<CancellationToken>()).Returns(role);

        return new AccountFixture(email, account.Id);
    }

    private AccountFixture ArrangeClientAccount(bool active)
    {
        var email = "cliente@huellitas.test";
        var user = new UserEntity("Cliente User", email, null, ClientRoleId);
        var account = new UserAccountEntity(
            user.Id,
            "cliente",
            email,
            active ? "Activo" : "Inactivo");
        var credentials = new UserCredentialsEntity(account.Id, PasswordHash);
        var role = new RoleEntity("Cliente", "Sin panel");

        userAccountRepository.GetByMailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        userAccountRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        userCredentialRepository.GetByAccountIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(credentials);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(ClientRoleId, Arg.Any<CancellationToken>()).Returns(role);

        return new AccountFixture(email, account.Id);
    }

    private sealed record AccountFixture(string Email, Guid AccountId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
