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
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.UserAccounts;

// P2 corregido: UserAccounts.Mail no tenía chequeo de duplicados (a
// diferencia de Username), así que dos cuentas podían compartir el mismo
// correo -- y LoginAsync resuelve la cuenta por GetByMailAsync().FirstOrDefault,
// dejando una de las dos inalcanzable por login.
public sealed class CreateUserAccountCommandHandlerTests
{
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateUserAccountCommandHandler sut;

    public CreateUserAccountCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        sut = new CreateUserAccountCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_mail_is_already_used_by_another_account()
    {
        var adminRole = new RoleEntity("Administrador", null);
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", adminRole.Id);

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        userAccountsRepository.ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountsRepository.ExistsByUsernameAsync("ana", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);

        var command = new CreateUserAccountCommand(user.Id, "ana", "ana@huellitas.test", "Activo");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await userAccountsRepository.DidNotReceive().AddAsync(
            Arg.Any<Domain.UserAccounts.Entities.UserAccounts>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_creates_the_account_when_username_and_mail_are_both_free()
    {
        var adminRole = new RoleEntity("Administrador", null);
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", adminRole.Id);

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        userAccountsRepository.ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountsRepository.ExistsByUsernameAsync("ana", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        var command = new CreateUserAccountCommand(user.Id, "ana", "ana@huellitas.test", "Activo");

        var accountId = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, accountId);
        await userAccountsRepository.Received(1).AddAsync(
            Arg.Any<Domain.UserAccounts.Entities.UserAccounts>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_forbidden_with_PlatformAccessDenied_when_user_role_is_Cliente()
    {
        // Cliente sin password: no debe poder asociar USER_ACCOUNTS.
        var clientRole = new RoleEntity("Cliente", null);
        var user = new UserEntity("Cliente Ana", "cliente@huellitas.test", null, clientRole.Id);

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);

        var command = new CreateUserAccountCommand(user.Id, "cliente", "cliente@huellitas.test", "Activo");

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, ex.Code);
        await userAccountsRepository.DidNotReceive().AddAsync(
            Arg.Any<Domain.UserAccounts.Entities.UserAccounts>(), Arg.Any<CancellationToken>());
        await userAccountsRepository.DidNotReceive().ExistsByUserIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task Handle_creates_account_when_user_role_is_Admin()
    {
        var adminRole = new RoleEntity("Administrador", null);
        var user = new UserEntity("Admin Ana", "admin@huellitas.test", "hash", adminRole.Id);

        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        userAccountsRepository.ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountsRepository.ExistsByUsernameAsync("admin", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        userAccountsRepository.ExistsByMailAsync("admin@huellitas.test", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        var command = new CreateUserAccountCommand(user.Id, "admin", "admin@huellitas.test", "Activo");

        var accountId = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, accountId);
        await userAccountsRepository.Received(1).AddAsync(
            Arg.Any<Domain.UserAccounts.Entities.UserAccounts>(), Arg.Any<CancellationToken>());
    }
}
