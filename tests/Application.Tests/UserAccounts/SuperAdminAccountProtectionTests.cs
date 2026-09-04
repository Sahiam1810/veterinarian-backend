using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserAccounts.UseCase;
using Application.Users.Abstraction;
using Domain.Roles;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.UserAccounts;

public sealed class SuperAdminAccountProtectionTests
{
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUserAccountsRepository accountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public SuperAdminAccountProtectionTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.UserAccountsRepository.Returns(accountsRepository);
    }

    [Fact]
    public async Task Update_rejects_changes_to_a_SuperAdmin_account()
    {
        var (user, account) = ConfigureSuperAdminAccount();
        var handler = new UpdateUserAccountCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new UpdateUserAccountCommand(account.Id, "root2", user.Email.Value, "Inactivo"),
            CancellationToken.None));

        await accountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_rejects_a_SuperAdmin_account()
    {
        var (_, account) = ConfigureSuperAdminAccount();
        var handler = new DeleteUserAccountCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new DeleteUserAccountCommand(account.Id),
            CancellationToken.None));

        await accountsRepository.DidNotReceive().DeleteAsync(
            Arg.Any<UserAccountEntity>(),
            Arg.Any<CancellationToken>());
    }

    private (UserEntity User, UserAccountEntity Account) ConfigureSuperAdminAccount()
    {
        var user = new UserEntity("Root", "root@huellitas.test", "hash", SystemRoles.SuperAdminId);
        var account = new UserAccountEntity(user.Id, "root", user.Email.Value, "Activo");

        accountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema"));

        return (user, account);
    }
}
