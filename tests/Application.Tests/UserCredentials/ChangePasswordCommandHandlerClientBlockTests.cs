using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.Security.Errors;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.UserCredentials.UseCase;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.UserCredentials;

public sealed class ChangePasswordCommandHandlerClientBlockTests
{
    private static readonly Guid ClientRoleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid StaffRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NonPanelRoleId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository userCredentialsRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ChangePasswordCommandHandler sut;

    public ChangePasswordCommandHandlerClientBlockTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.UserCredentialsRepository.Returns(userCredentialsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        rolesRepository.GetByIdAsync(ClientRoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Cliente", null));
        rolesRepository.GetByIdAsync(StaffRoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Administrador", null));
        rolesRepository.GetByIdAsync(NonPanelRoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("RolExterno", null));
        sut = new ChangePasswordCommandHandler(unitOfWork, passwordHasher);
    }

    [Fact]
    public async Task Handle_throws_forbidden_with_PlatformAccessDenied_when_updating_credentials_of_Cliente()
    {
        var user = new UserEntity("Cliente", "cliente@huellitas.test", null, ClientRoleId);
        var account = new UserAccountEntity(user.Id, "cliente", "cliente@huellitas.test", "Activo");
        var credentials = new UserCredentialsEntity(account.Id, "old-hash");
        userCredentialsRepository.GetByIdAsync(credentials.Id, Arg.Any<CancellationToken>())
            .Returns(credentials);
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(
                new ChangePasswordCommand(credentials.Id, "current", "new-password-1"),
                CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Description, ex.Message);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, ex.Code);
        await userCredentialsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserCredentialsEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_forbidden_with_PlatformAccessDenied_when_role_is_not_web_panel_allowed()
    {
        var user = new UserEntity("Externo", "externo@huellitas.test", "hash", NonPanelRoleId);
        var account = new UserAccountEntity(user.Id, "externo", "externo@huellitas.test", "Activo");
        var credentials = new UserCredentialsEntity(account.Id, "old-hash");
        userCredentialsRepository.GetByIdAsync(credentials.Id, Arg.Any<CancellationToken>())
            .Returns(credentials);
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(
                new ChangePasswordCommand(credentials.Id, "current", "new-password-1"),
                CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, ex.Code);
        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Description, ex.Message);
        await userCredentialsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserCredentialsEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_forbidden_with_PlatformAccessDenied_when_account_is_inactive()
    {
        var user = new UserEntity("Staff", "staff@huellitas.test", "hash", StaffRoleId);
        var account = new UserAccountEntity(user.Id, "staff", "staff@huellitas.test", "Inactivo");
        var credentials = new UserCredentialsEntity(account.Id, "old-hash");
        userCredentialsRepository.GetByIdAsync(credentials.Id, Arg.Any<CancellationToken>())
            .Returns(credentials);
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.Handle(
                new ChangePasswordCommand(credentials.Id, "current", "new-password-1"),
                CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, ex.Code);
        await userCredentialsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserCredentialsEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_updates_credentials_for_Staff()
    {
        var user = new UserEntity("Staff", "staff@huellitas.test", "hash", StaffRoleId);
        var account = new UserAccountEntity(user.Id, "staff", "staff@huellitas.test", "Activo");
        var credentials = new UserCredentialsEntity(account.Id, "old-hash");
        userCredentialsRepository.GetByIdAsync(credentials.Id, Arg.Any<CancellationToken>())
            .Returns(credentials);
        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Verify("current", "old-hash").Returns(true);
        passwordHasher.Hash("new-password-1").Returns("new-hash");

        await sut.Handle(
            new ChangePasswordCommand(credentials.Id, "current", "new-password-1"),
            CancellationToken.None);

        Assert.Equal("new-hash", credentials.PasswordHash);
        await userCredentialsRepository.Received(1).UpdateAsync(
            credentials, Arg.Any<CancellationToken>());
    }
}
