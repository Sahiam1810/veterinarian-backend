using System.IdentityModel.Tokens.Jwt;
using Api.Tests.Support;
using Application.Common.Abstractions;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Domain.Roles;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Api.Tests.Security;

// U5: USERS ya trae la contraseña directamente -- no hay cuenta ni
// credenciales separadas que resolver.
public sealed class AuthenticationServiceSuperAdminTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(
        2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    private const string SuperAdminEmail = "superadmin@huellitas.test";
    private const string SuperAdminPasswordHash = "stored-hash";

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
            usersRepository,
            tokenRepository,
            unitOfWork,
            Substitute.For<ISender>(),
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
        var user = ConfigurePersistedSuperAdmin();
        passwordHasher.Verify("correct-password", SuperAdminPasswordHash).Returns(true);

        var result = await sut.LoginAsync(
            SuperAdminEmail.ToUpperInvariant(),
            "correct-password",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.RefreshToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == "person_id").Value);
        Assert.Equal(SystemRoles.SuperAdminId.ToString(), jwt.Claims.Single(c => c.Type == "role_id").Value);
        Assert.Equal(SystemRoles.SuperAdminName, jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "super_admin");
        await tokenRepository.Received(1).AddAsync(
            Arg.Any<UserTokenEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_with_wrong_password_uses_the_persisted_user_and_fails()
    {
        ConfigurePersistedSuperAdmin();
        passwordHasher.Verify("wrong-password", SuperAdminPasswordHash).Returns(false);

        var result = await sut.LoginAsync(
            SuperAdminEmail,
            "wrong-password",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidCredentials, result.Error);
        await usersRepository.Received(1)
            .GetByEmailAsync(SuperAdminEmail, Arg.Any<CancellationToken>());
        await tokenRepository.DidNotReceive().AddAsync(
            Arg.Any<UserTokenEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentProfileAsync_returns_the_persisted_SuperAdmin_profile()
    {
        var user = ConfigurePersistedSuperAdmin();

        var result = await sut.GetCurrentProfileAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(SystemRoles.SuperAdminName, result.Value.Role);
        await usersRepository.Received(1)
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>());
    }

    private UserEntity ConfigurePersistedSuperAdmin()
    {
        var user = new UserEntity(
            "Super Administrador",
            SuperAdminEmail,
            SuperAdminPasswordHash,
            SystemRoles.SuperAdminId);
        var role = new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema");

        usersRepository.GetByEmailAsync(SuperAdminEmail, Arg.Any<CancellationToken>()).Returns(user);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>()).Returns(role);

        return user;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
