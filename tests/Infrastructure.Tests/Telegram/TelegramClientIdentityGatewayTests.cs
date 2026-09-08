using Application.Clients.Abstraction;
using Application.Roles.Abstraction;
using Application.Telegram.Errors;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Telegram.Identity;
using NSubstitute;
using Xunit;
using ClientEntity = Domain.Clients.Entities.ClientEntity;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramClientIdentityGatewayTests
{
    [Fact]
    public async Task Active_client_is_resolved_by_identification()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("Cliente", null);
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
        var account = new UserAccountEntity(user.Id, "tg_ana", "ana@example.test", "Activo");
        var client = new ClientEntity(user.Id, "123456789", null);
        clients.GetByIdentificationNumberAsync("123456789", default).Returns(client);
        users.GetByIdAsync(user.Id, default).Returns(user);
        accounts.GetByUserIdAsync(user.Id, default).Returns(account);
        roles.GetByIdAsync(role.Id, default).Returns(role);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var result = await gateway.FindActiveByIdentificationAsync("123456789", default);

        Assert.Equal(new TelegramClientIdentity(user.Id, account.Id, "ana@example.test"), result);
    }

    [Fact]
    public async Task Active_client_with_administrative_role_is_resolved_by_identification()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("SuperAdmin", null);
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
        var account = new UserAccountEntity(user.Id, "admin_ana", "ana@example.test", "Activo");
        var client = new ClientEntity(user.Id, "123456789", null);
        clients.GetByIdentificationNumberAsync("123456789", default).Returns(client);
        users.GetByIdAsync(user.Id, default).Returns(user);
        accounts.GetByUserIdAsync(user.Id, default).Returns(account);
        roles.GetByIdAsync(role.Id, default).Returns(role);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var result = await gateway.FindActiveByIdentificationAsync("123456789", default);

        Assert.Equal(new TelegramClientIdentity(user.Id, account.Id, "ana@example.test"), result);
    }

    [Fact]
    public async Task Registration_stages_passwordless_client_records()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("Cliente", null);
        roles.GetByNameAsync("Cliente", default).Returns(role);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var result = await gateway.CompleteRegistrationAsync(
            new TelegramClientRegistration("123456789", "Ana Pérez", "ana@example.test"),
            null,
            default);

        Assert.Equal("ana@example.test", result.Email);
        await users.Received(1).AddAsync(
            Arg.Is<UserEntity>(user => user.PasswordHash == null && user.RoleId == role.Id),
            default);
        await accounts.Received(1).AddAsync(
            Arg.Is<UserAccountEntity>(account =>
                account.UserId == result.PersonId && account.Status == "Activo"),
            default);
        await clients.Received(1).AddAsync(
            Arg.Is<ClientEntity>(client => client.UserId == result.PersonId),
            default);
    }

    [Fact]
    public async Task Registration_with_existing_identity_raises_registration_conflict()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        roles.GetByNameAsync("Cliente", default).Returns(new RoleEntity("Cliente", null));
        clients.ExistsByIdentificationNumberAsync("123456789", default).Returns(true);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var action = () => gateway.CompleteRegistrationAsync(
            new TelegramClientRegistration("123456789", "Ana Pérez", "ana@example.test"),
            null,
            default);

        await Assert.ThrowsAsync<TelegramRegistrationConflictException>(action);
    }

    [Fact]
    public async Task Active_account_without_client_receives_only_client_profile()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("Administrador", null);
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
        var account = new UserAccountEntity(user.Id, "ana", "ana@example.test", "Activo");
        users.GetByIdAsync(user.Id, default).Returns(user);
        accounts.GetByUserIdAsync(user.Id, default).Returns(account);
        clients.GetByUserIdAsync(user.Id, default).Returns((ClientEntity?)null);
        clients.ExistsByIdentificationNumberAsync("123456789", default).Returns(false);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var result = await gateway.CompleteRegistrationAsync(
            new TelegramClientRegistration("123456789", "Ignored Name", "ana@example.test"),
            user.Id,
            default);

        Assert.Equal(new TelegramClientIdentity(user.Id, account.Id, "ana@example.test"), result);
        await clients.Received(1).AddAsync(
            Arg.Is<ClientEntity>(client =>
                client.UserId == user.Id &&
                client.IdentificationNumber.Value == "123456789"),
            default);
        await users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await accounts.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Active_account_with_matching_client_is_reused_without_duplicates()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("Cliente", null);
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
        var account = new UserAccountEntity(user.Id, "ana", "ana@example.test", "Activo");
        var client = new ClientEntity(user.Id, "123456789", null);
        users.GetByIdAsync(user.Id, default).Returns(user);
        accounts.GetByUserIdAsync(user.Id, default).Returns(account);
        clients.GetByUserIdAsync(user.Id, default).Returns(client);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var result = await gateway.CompleteRegistrationAsync(
            new TelegramClientRegistration("123456789", "Ignored Name", "ana@example.test"),
            user.Id,
            default);

        Assert.Equal(new TelegramClientIdentity(user.Id, account.Id, "ana@example.test"), result);
        await clients.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Active_account_with_different_identification_is_rejected()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("Cliente", null);
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
        var account = new UserAccountEntity(user.Id, "ana", "ana@example.test", "Activo");
        users.GetByIdAsync(user.Id, default).Returns(user);
        accounts.GetByUserIdAsync(user.Id, default).Returns(account);
        clients.GetByUserIdAsync(user.Id, default)
            .Returns(new ClientEntity(user.Id, "987654321", null));
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var action = () => gateway.CompleteRegistrationAsync(
            new TelegramClientRegistration("123456789", "Ignored Name", "ana@example.test"),
            user.Id,
            default);

        await Assert.ThrowsAsync<TelegramClientIdentificationMismatchException>(action);
        await clients.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Identification_owned_by_another_person_is_rejected()
    {
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var roles = Substitute.For<IRolesRepository>();
        var role = new RoleEntity("Cliente", null);
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
        var account = new UserAccountEntity(user.Id, "ana", "ana@example.test", "Activo");
        users.GetByIdAsync(user.Id, default).Returns(user);
        accounts.GetByUserIdAsync(user.Id, default).Returns(account);
        clients.GetByUserIdAsync(user.Id, default).Returns((ClientEntity?)null);
        clients.ExistsByIdentificationNumberAsync("123456789", default).Returns(true);
        var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

        var action = () => gateway.CompleteRegistrationAsync(
            new TelegramClientRegistration("123456789", "Ignored Name", "ana@example.test"),
            user.Id,
            default);

        await Assert.ThrowsAsync<TelegramClientIdentificationMismatchException>(action);
        await clients.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
