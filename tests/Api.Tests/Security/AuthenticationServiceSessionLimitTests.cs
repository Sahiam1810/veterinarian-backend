using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Permissions.UseCases;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Api.Tests.Security;

public sealed class AuthenticationServiceSessionLimitTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid StaffRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private const string Password = "CorrectPassword1!";
    private const string PasswordHash = "stored-hash";
    private const string RawRefreshToken = "raw-refresh-token";

    private readonly IUserAccountsRepository userAccountRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository userCredentialRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUserTokensRepository userTokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly JwtRsaKeyMaterial keyMaterial;
    private readonly MutableTimeProvider clock = new(T0);
    private readonly AuthenticationService sut;
    private readonly RefreshTokenProtector protector = new();

    public AuthenticationServiceSessionLimitTests()
    {
        var keys = RsaTestKeys.Create();
        var jwtOptions = new JwtOptions
        {
            Issuer = "Veterinaria.Api.Tests",
            Audience = "Veterinaria.Client.Tests",
            PrivateKeyPemBase64 = keys.PrivateKeyPemBase64,
            PublicKeyPemBase64 = keys.PublicKeyPemBase64,
            KeyId = "session-limit-test-key",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
            MaxSessionHours = 24,
            ClockSkewSeconds = 0
        };
        keyMaterial = new JwtRsaKeyMaterial(Options.Create(jwtOptions));
        var jwtTokenIssuer = new JwtTokenIssuer(
            Options.Create(jwtOptions), keyMaterial, clock);

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
            protector,
            passwordHasher,
            Options.Create(jwtOptions),
            clock);
    }

    public void Dispose() => keyMaterial.Dispose();

    [Fact]
    public async Task B1_Login_sets_SessionStartedAt_to_fake_T0()
    {
        var fixture = ArrangeStaffAccount();
        passwordHasher.Verify(Password, PasswordHash).Returns(true);
        UserTokenEntity? persisted = null;
        userTokenRepository
            .When(repository => repository.AddAsync(
                Arg.Any<UserTokenEntity>(),
                Arg.Any<CancellationToken>()))
            .Do(call => persisted = call.ArgAt<UserTokenEntity>(0));

        var result = await sut.LoginAsync(fixture.Email, Password, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(T0.UtcDateTime, persisted!.SessionStartedAt);
    }

    [Fact]
    public async Task B2_Refresh_before_24h_preserves_SessionStartedAt_T0()
    {
        var fixture = ArrangeStaffAccount();
        ArrangeRefreshToken(
            fixture.AccountId,
            sessionStartedAt: T0.UtcDateTime,
            expiresAt: T0.AddDays(7).UtcDateTime);

        clock.SetUtcNow(T0.AddHours(23));
        UserTokenEntity? replacement = null;
        userTokenRepository
            .When(repository => repository.AddAsync(
                Arg.Any<UserTokenEntity>(),
                Arg.Any<CancellationToken>()))
            .Do(call => replacement = call.ArgAt<UserTokenEntity>(0));

        var result = await sut.RefreshAsync(RawRefreshToken, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(replacement);
        Assert.Equal(T0.UtcDateTime, replacement!.SessionStartedAt);
        await userTokenRepository.Received(1)
            .DeleteAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
        await userTokenRepository.Received(1)
            .AddAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task B3_Refresh_exactly_at_T0_plus_24h_returns_InvalidRefreshToken_without_rotation()
    {
        var fixture = ArrangeStaffAccount();
        ArrangeRefreshToken(
            fixture.AccountId,
            sessionStartedAt: T0.UtcDateTime,
            expiresAt: T0.AddDays(7).UtcDateTime);

        clock.SetUtcNow(T0.AddHours(24));

        var result = await sut.RefreshAsync(RawRefreshToken, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRefreshToken, result.Error);
        await userTokenRepository.DidNotReceive()
            .DeleteAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task B4_Refresh_after_24h_returns_InvalidRefreshToken_even_if_ExpiresAt_is_future()
    {
        var fixture = ArrangeStaffAccount();
        ArrangeRefreshToken(
            fixture.AccountId,
            sessionStartedAt: T0.UtcDateTime,
            expiresAt: T0.AddDays(7).UtcDateTime);

        clock.SetUtcNow(T0.AddHours(25));

        var result = await sut.RefreshAsync(RawRefreshToken, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRefreshToken, result.Error);
        await userTokenRepository.DidNotReceive()
            .DeleteAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task B6_Refresh_missing_token_keeps_InvalidRefreshToken()
    {
        userTokenRepository
            .GetByTokenValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserTokenEntity?)null);

        var result = await sut.RefreshAsync("missing-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRefreshToken, result.Error);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task B6_Refresh_expired_individual_token_keeps_InvalidRefreshToken()
    {
        var fixture = ArrangeStaffAccount();
        ArrangeRefreshToken(
            fixture.AccountId,
            sessionStartedAt: T0.UtcDateTime,
            expiresAt: T0.AddMinutes(30).UtcDateTime);

        clock.SetUtcNow(T0.AddHours(1));

        var result = await sut.RefreshAsync(RawRefreshToken, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRefreshToken, result.Error);
        await userTokenRepository.DidNotReceive()
            .AddAsync(Arg.Any<UserTokenEntity>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeRefreshToken(
        Guid accountId,
        DateTime sessionStartedAt,
        DateTime expiresAt)
    {
        var hash = protector.Hash(RawRefreshToken);
        var token = new UserTokenEntity(
            accountId,
            hash,
            "refresh",
            expiresAt,
            sessionStartedAt);

        userTokenRepository
            .GetByTokenValueAsync(hash, Arg.Any<CancellationToken>())
            .Returns(token);
    }

    private AccountFixture ArrangeStaffAccount()
    {
        var email = "staff@huellitas.test";
        var user = new UserEntity("Staff User", email, PasswordHash, StaffRoleId);
        var account = new UserAccountEntity(user.Id, "staff", email, "Activo");
        var credentials = new UserCredentialsEntity(account.Id, PasswordHash);
        var role = new RoleEntity("Administrador", "Staff panel");

        userAccountRepository.GetByMailAsync(email, Arg.Any<CancellationToken>()).Returns(account);
        userAccountRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        userCredentialRepository.GetByAccountIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(credentials);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(StaffRoleId, Arg.Any<CancellationToken>()).Returns(role);

        return new AccountFixture(email, account.Id);
    }

    private sealed record AccountFixture(string Email, Guid AccountId);

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public void SetUtcNow(DateTimeOffset value) => current = value;

        public override DateTimeOffset GetUtcNow() => current;
    }
}
