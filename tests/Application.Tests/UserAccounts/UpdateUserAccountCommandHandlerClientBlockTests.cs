using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserAccounts.UseCase;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.UserAccounts;

public sealed class UpdateUserAccountCommandHandlerClientBlockTests
{
    private static readonly Guid StaffRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ClientRoleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserAccountCommandHandler sut;

    public UpdateUserAccountCommandHandlerClientBlockTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        rolesRepository.GetByIdAsync(StaffRoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Administrador", null));
        rolesRepository.GetByIdAsync(ClientRoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Cliente", null));
        sut = new UpdateUserAccountCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_forbidden_with_PlatformAccessDenied_code_when_account_is_Cliente()
    {
        var user = new UserEntity("Cliente", "cliente@huellitas.test", null, ClientRoleId);
        var account = new UserAccountEntity(user.Id, "cliente", "cliente@huellitas.test", "Activo");
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(
                new UpdateUserAccountCommand(account.Id, "cliente2", "cliente2@huellitas.test", "Activo"),
                CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, ex.Code);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Description, ex.Message);
        await userAccountsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserAccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_updates_Staff_account()
    {
        var user = new UserEntity("Staff", "staff@huellitas.test", "hash", StaffRoleId);
        var account = new UserAccountEntity(user.Id, "staff", "staff@huellitas.test", "Activo");
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        userAccountsRepository.ExistsByUsernameAsync("staff2", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("staff2@huellitas.test", Arg.Any<CancellationToken>(), account.Id)
            .Returns(false);

        await sut.Handle(
            new UpdateUserAccountCommand(account.Id, "staff2", "staff2@huellitas.test", "Activo"),
            CancellationToken.None);

        await userAccountsRepository.Received(1).UpdateAsync(account, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
