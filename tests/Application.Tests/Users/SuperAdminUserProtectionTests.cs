using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Domain.Roles;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

public sealed class SuperAdminUserProtectionTests
{
    private static readonly Guid AdministratorRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();

    public SuperAdminUserProtectionTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
    }

    [Fact]
    public async Task Create_rejects_assignment_of_the_SuperAdmin_role()
    {
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema"));
        var handler = new CreateUserCommandHandler(unitOfWork, passwordHasher);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new CreateUserCommand(
                "Root",
                "root@huellitas.test",
                "ValidPassword!1",
                SystemRoles.SuperAdminId),
            CancellationToken.None));

        await usersRepository.DidNotReceive().AddAsync(
            Arg.Any<UserEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_rejects_promotion_to_the_SuperAdmin_role()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", AdministratorRoleId);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema"));
        var handler = new UpdateUserCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new UpdateUserCommand(user.Id, user.FullName, user.Email.Value, SystemRoles.SuperAdminId),
            CancellationToken.None));
    }

    [Fact]
    public async Task Update_rejects_demotion_of_a_SuperAdmin_user()
    {
        var user = new UserEntity("Root", "root@huellitas.test", "hash", SystemRoles.SuperAdminId);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        rolesRepository.GetByIdAsync(AdministratorRoleId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity("Administrador", null));
        var handler = new UpdateUserCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new UpdateUserCommand(user.Id, user.FullName, user.Email.Value, AdministratorRoleId),
            CancellationToken.None));
    }

    [Fact]
    public async Task Deactivate_rejects_a_SuperAdmin_user()
    {
        var user = new UserEntity("Root", "root@huellitas.test", "hash", SystemRoles.SuperAdminId);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new DeactivateUserCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new DeactivateUserCommand(user.Id),
            CancellationToken.None));

        Assert.True(user.IsActive);
    }
}
