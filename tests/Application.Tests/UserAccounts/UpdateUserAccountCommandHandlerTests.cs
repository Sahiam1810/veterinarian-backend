using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserAccounts.Errors;
using Application.UserAccounts.UseCase;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.UserAccounts;

public sealed class UpdateUserAccountCommandHandlerTests
{
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserAccountCommandHandler sut;

    public UpdateUserAccountCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        sut = new UpdateUserAccountCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_mail_is_already_used_by_a_different_account()
    {
        var adminRole = new RoleEntity("Administrador", null);
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", adminRole.Id);
        var account = new UserAccountEntity(user.Id, "ana", "ana@huellitas.test", "Activo");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        userAccountsRepository.ExistsByUsernameAsync("ana", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("otra@huellitas.test", Arg.Any<CancellationToken>(), account.Id)
            .Returns(true);

        var command = new UpdateUserAccountCommand(account.Id, "ana", "otra@huellitas.test", "Activo");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await userAccountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_allows_keeping_the_accounts_own_current_mail()
    {
        var adminRole = new RoleEntity("Administrador", null);
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", adminRole.Id);
        var account = new UserAccountEntity(user.Id, "ana", "ana@huellitas.test", "Activo");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        userAccountsRepository.ExistsByUsernameAsync("ana", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);

        var command = new UpdateUserAccountCommand(account.Id, "ana", "ana@huellitas.test", "Inactivo");

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal("Inactivo", account.Status);
        await userAccountsRepository.Received(1).UpdateAsync(account, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_conflict_with_stable_code_when_account_user_is_Cliente()
    {
        var clientRole = new RoleEntity("Cliente", null);
        var user = new UserEntity("Cliente Ana", "cliente@huellitas.test", null, clientRole.Id);
        var account = new UserAccountEntity(user.Id, "cliente", "cliente@huellitas.test", "Inactivo");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);

        // Reactivar no debe abrirse para Cliente.
        var command = new UpdateUserAccountCommand(account.Id, "cliente", "cliente@huellitas.test", "Activo");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal(UserAccountErrorCodes.ClientCannotHaveLogin, ex.Code);
        await userAccountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
    }
}
