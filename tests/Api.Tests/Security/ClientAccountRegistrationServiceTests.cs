using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Roles.Abstraction;
using Application.Security.Abstractions;
using Application.Security.Errors;
using Application.Security.Registration;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Authentication;
using Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using ClientEntity = Domain.Clients.Entities.ClientEntity;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Security;

public sealed class ClientAccountRegistrationServiceTests
{
    private static readonly ClientAccountRegistrationRequest ValidRequest = new(
        "Ana Cliente",
        "ANA@HUELLITAS.TEST",
        "Ana.Cliente",
        "Password123!",
        "1234567890");

    private readonly IUserAccountsRepository accounts = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository credentials = Substitute.For<IUserCredentialsRepository>();
    private readonly IUsersRepository users = Substitute.For<IUsersRepository>();
    private readonly IClientRepository clients = Substitute.For<IClientRepository>();
    private readonly IRolesRepository roles = Substitute.For<IRolesRepository>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ClientAccountRegistrationService sut;

    public ClientAccountRegistrationServiceTests()
    {
        roles.GetByNameAsync("Cliente", Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Cliente", null));
        users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        accounts.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        accounts.GetByMailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);
        clients.ExistsByIdentificationNumberAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        sut = new ClientAccountRegistrationService(
            accounts,
            credentials,
            users,
            clients,
            roles,
            passwordHasher,
            Options.Create(new SuperAdminOptions
            {
                Enabled = true,
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Email = "superadmin@huellitas.test",
                PasswordHash = "stored-hash"
            }));
    }

    [Fact]
    public async Task StageAsync_stages_complete_client_account_without_saving()
    {
        var result = await sut.StageAsync(ValidRequest, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ana@huellitas.test", result.Value.Email);
        Assert.Equal("ana.cliente", result.Value.UserName);
        await users.Received(1).AddAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
        await accounts.Received(1).AddAsync(Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
        await credentials.Received(1).AddAsync(Arg.Any<UserCredentialEntity>(), Arg.Any<CancellationToken>());
        await clients.Received(1).AddAsync(
            Arg.Is<ClientEntity>(client => client.UserId == result.Value.PersonId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StageAsync_rejects_an_existing_identification_before_staging_entities()
    {
        clients.ExistsByIdentificationNumberAsync(
                "1234567890", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);

        var result = await sut.StageAsync(ValidRequest, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.IdentificationNumberAlreadyExists, result.Error);
        await users.DidNotReceive().AddAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
        await clients.DidNotReceive().AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StageAsync_rejects_blank_required_data()
    {
        var result = await sut.StageAsync(
            ValidRequest with { IdentificationNumber = " " },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.InvalidRegistrationData, result.Error);
        await roles.DidNotReceive().GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
