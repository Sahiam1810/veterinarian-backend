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
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.UserCredentials;

// 1.4: no fabricar USER_CREDENTIALS para rol Cliente.
public sealed class CreateUserCredentialsCommandHandlerTests
{
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IUserCredentialsRepository userCredentialsRepository = Substitute.For<IUserCredentialsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly CreateUserCredentialsCommandHandler sut;

    public CreateUserCredentialsCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.UserCredentialsRepository.Returns(userCredentialsRepository);
        sut = new CreateUserCredentialsCommandHandler(unitOfWork, passwordHasher);
    }

    [Fact]
    public async Task Handle_throws_forbidden_with_PlatformAccessDenied_when_account_user_is_Cliente()
    {
        var clientRole = new RoleEntity("Cliente", null);
        var user = new UserEntity("Cliente Ana", "cliente@huellitas.test", null, clientRole.Id);
        var account = new UserAccountEntity(user.Id, "cliente", "cliente@huellitas.test", "Activo");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);

        var command = new CreateUserCredentialsCommand(account.Id, "Password123!");

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.Handle(command, CancellationToken.None));

        Assert.Equal(AuthenticationErrors.PlatformAccessDenied.Code, ex.Code);
        await userCredentialsRepository.DidNotReceive().AddAsync(
            Arg.Any<Domain.UserCredentials.Entities.UserCredentials>(), Arg.Any<CancellationToken>());
        await userCredentialsRepository.DidNotReceive().ExistsByAccountIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_creates_credentials_when_account_user_is_Admin()
    {
        var adminRole = new RoleEntity("Administrador", null);
        var user = new UserEntity("Admin Ana", "admin@huellitas.test", "hash", adminRole.Id);
        var account = new UserAccountEntity(user.Id, "admin", "admin@huellitas.test", "Activo");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        userCredentialsRepository.ExistsByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        passwordHasher.Hash("Password123!").Returns("hashed");

        var command = new CreateUserCredentialsCommand(account.Id, "Password123!");

        var credentialId = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, credentialId);
        await userCredentialsRepository.Received(1).AddAsync(
            Arg.Any<Domain.UserCredentials.Entities.UserCredentials>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
