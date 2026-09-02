using System.IdentityModel.Tokens.Jwt;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Security.Errors;
using Application.Security.Registration;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

public sealed class AuthenticationServiceSuperAdminTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(
        2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid SuperAdminId = Guid.Parse(
        "99999999-9999-9999-9999-999999999999");

    private const string SuperAdminEmail = "superadmin@huellitas.test";
    private const string SuperAdminPasswordHash = "stored-hash";

    private readonly IUserAccountsRepository userAccountRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository userCredentialRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUserTokensRepository userTokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IClientAccountRegistrationService registration =
        Substitute.For<IClientAccountRegistrationService>();
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

        var superAdminOptions = new SuperAdminOptions
        {
            Enabled = true,
            Id = SuperAdminId,
            Email = SuperAdminEmail,
            PasswordHash = SuperAdminPasswordHash
        };
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(CancellationToken.None));
        registration.StageAsync(
                Arg.Is<ClientAccountRegistrationRequest>(request =>
                    request.Email.Equals(SuperAdminEmail, StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
            .Returns(Application.Common.Results.Result<RegisteredClientAccount>.Failure(
                AuthenticationErrors.UserAlreadyExists));

        sut = new AuthenticationService(
            userAccountRepository,
            userCredentialRepository,
            userTokenRepository,
            usersRepository,
            registration,
            unitOfWork,
            jwtTokenIssuer,
            new RefreshTokenProtector(),
            passwordHasher,
            Options.Create(jwtOptions),
            Options.Create(superAdminOptions),
            new FixedTimeProvider(Now));
    }

    public void Dispose() => keyMaterial.Dispose();

    [Fact]
    public async Task LoginAsync_with_superadmin_email_and_correct_password_issues_token_without_touching_user_tables()
    {
        passwordHasher.Verify(Arg.Any<string>(), SuperAdminPasswordHash).Returns(true);

        var result = await sut.LoginAsync(
            SuperAdminEmail.ToUpperInvariant(), "whatever-password", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value.RefreshToken);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal("true", token.Claims.Single(c => c.Type == "super_admin").Value);
        Assert.DoesNotContain(token.Claims, c => c.Type is "role_id" or "role");

        await userAccountRepository.DidNotReceive().GetByMailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await userTokenRepository.DidNotReceive().AddAsync(Arg.Any<Domain.UserTokens.Entities.UserTokens>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_with_superadmin_email_and_wrong_password_fails_without_touching_user_tables()
    {
        passwordHasher.Verify(Arg.Any<string>(), SuperAdminPasswordHash).Returns(false);

        var result = await sut.LoginAsync(SuperAdminEmail, "wrong-password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);

        await userAccountRepository.DidNotReceive().GetByMailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_with_a_different_email_falls_through_to_the_normal_flow()
    {
        userAccountRepository
            .GetByMailAsync("regular.user@huellitas.test", Arg.Any<CancellationToken>())
            .Returns((Domain.UserAccounts.Entities.UserAccounts?)null);

        var result = await sut.LoginAsync(
            "regular.user@huellitas.test", "whatever-password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);

        await userAccountRepository.Received(1)
            .GetByMailAsync("regular.user@huellitas.test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_with_superadmin_email_fails_without_creating_a_shadow_account()
    {
        var result = await sut.RegisterAsync(
            "Someone",
            SuperAdminEmail.ToUpperInvariant(),
            "someone",
            "Password123!",
            "1234567890",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.UserAlreadyExists, result.Error);

        await usersRepository.DidNotReceive().ExistsByEmailAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>());
        await usersRepository.DidNotReceive().AddAsync(
            Arg.Any<Domain.Users.Entities.Users>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentProfileAsync_with_superadmin_id_returns_a_synthetic_profile()
    {
        var result = await sut.GetCurrentProfileAsync(SuperAdminId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SuperAdminEmail, result.Value.Email);
        Assert.Equal("SuperAdmin", result.Value.Role);
        Assert.Equal(SuperAdminId, result.Value.UserAccountId);

        await userAccountRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
