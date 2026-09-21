using Application.Common.Abstractions;
using Application.Roles.Abstraction;
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

// T10: un rol llamado "Cliente" no tiene trato especial al crear credenciales.
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
    public async Task Handle_creates_credentials_when_account_user_role_is_named_Cliente()
    {
        var clientRole = new RoleEntity("Cliente", null);
        var user = new UserEntity("Cliente Ana", "cliente@huellitas.test", "hash", clientRole.Id);
        var account = new UserAccountEntity(user.Id, "cliente", "cliente@huellitas.test", "Activo");

        userAccountsRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(clientRole.Id, Arg.Any<CancellationToken>()).Returns(clientRole);
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
