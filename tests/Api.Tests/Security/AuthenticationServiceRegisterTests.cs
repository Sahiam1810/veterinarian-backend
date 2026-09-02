using Api.Tests.Support;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Roles.Abstraction;
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
using ClientEntity = Domain.Clients.Entities.ClientEntity;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Security;

// SEC-03 (pedido por la líder): el registro público solo creaba
// Users/UserAccounts/UserCredentials y nunca un Client, dejando al usuario
// auto-registrado sin perfil para /clients/me, /pets/mine, /appointments/mine.
public sealed class AuthenticationServiceRegisterTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
    private const string IdentificationNumber = "1234567890";

    private readonly IUserAccountsRepository userAccountRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository userCredentialRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUserTokensRepository userTokenRepository = Substitute.For<IUserTokensRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly JwtRsaKeyMaterial keyMaterial;
    private readonly AuthenticationService sut;

    public AuthenticationServiceRegisterTests()
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
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Email = "superadmin@huellitas.test",
            PasswordHash = "stored-hash"
        };

        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        rolesRepository
            .GetByNameAsync("Cliente", Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Cliente", null));

        usersRepository
            .ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountRepository
            .ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountRepository
            .GetByMailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);
        passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        sut = new AuthenticationService(
            userAccountRepository,
            userCredentialRepository,
            userTokenRepository,
            usersRepository,
            new ClientAccountRegistrationService(
                userAccountRepository,
                userCredentialRepository,
                usersRepository,
                clientsRepository,
                rolesRepository,
                passwordHasher,
                Options.Create(superAdminOptions)),
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
    public async Task RegisterAsync_creates_a_client_profile_with_the_provided_identification_number()
    {
        clientsRepository
            .ExistsByIdentificationNumberAsync(IdentificationNumber, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await sut.RegisterAsync(
            "Ana Cliente",
            "ana.cliente@huellitas.test",
            "ana.cliente",
            "Password123!",
            IdentificationNumber,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await clientsRepository.Received(1).AddAsync(
            Arg.Is<ClientEntity>(client => client.IdentificationNumber.Value == IdentificationNumber),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_links_the_created_client_to_the_created_user()
    {
        clientsRepository
            .ExistsByIdentificationNumberAsync(IdentificationNumber, Arg.Any<CancellationToken>())
            .Returns(false);
        UserEntity? createdUser = null;
        usersRepository
            .AddAsync(Arg.Do<UserEntity>(user => createdUser = user), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await sut.RegisterAsync(
            "Ana Cliente",
            "ana.cliente@huellitas.test",
            "ana.cliente",
            "Password123!",
            IdentificationNumber,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(createdUser);
        await clientsRepository.Received(1).AddAsync(
            Arg.Is<ClientEntity>(client => client.UserId == createdUser!.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_fails_when_the_identification_number_is_already_used_by_another_client()
    {
        clientsRepository
            .ExistsByIdentificationNumberAsync(IdentificationNumber, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await sut.RegisterAsync(
            "Ana Cliente",
            "ana.cliente@huellitas.test",
            "ana.cliente",
            "Password123!",
            IdentificationNumber,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.IdentificationNumberAlreadyExists, result.Error);
        await usersRepository.DidNotReceive().AddAsync(
            Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
        await clientsRepository.DidNotReceive().AddAsync(
            Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_fails_when_identification_number_is_blank(string identificationNumber)
    {
        var result = await sut.RegisterAsync(
            "Ana Cliente",
            "ana.cliente@huellitas.test",
            "ana.cliente",
            "Password123!",
            identificationNumber,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRegistrationData, result.Error);
        await clientsRepository.DidNotReceive().ExistsByIdentificationNumberAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
