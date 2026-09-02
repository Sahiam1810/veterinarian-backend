using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Modules.Abstraction;
using Application.UserPermissions.Abstraction;
using Application.UserPermissions.UseCases;
using Application.Users.Abstraction;
using NSubstitute;
using Xunit;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using UserEntity = Domain.Users.Entities.Users;
using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;

namespace Application.Tests.UserPermissions;

public sealed class CreateUserPermissionCommandHandlerTests
{
    private static readonly Guid RoleId = Guid.NewGuid();

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IModulesRepository modulesRepository = Substitute.For<IModulesRepository>();
    private readonly IUserPermissionsRepository userPermissionsRepository = Substitute.For<IUserPermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateUserPermissionCommandHandler sut;

    public CreateUserPermissionCommandHandlerTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.ModulesRepository.Returns(modulesRepository);
        unitOfWork.UserPermissionsRepository.Returns(userPermissionsRepository);
        sut = new CreateUserPermissionCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_user_does_not_exist()
    {
        var userId = Guid.NewGuid();
        usersRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

        var command = new CreateUserPermissionCommand(userId, Guid.NewGuid(), true, false, false, false);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await userPermissionsRepository.DidNotReceive().AddAsync(
            Arg.Any<UserPermissionEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_not_found_when_module_does_not_exist()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        modulesRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ModuleEntity?)null);

        var command = new CreateUserPermissionCommand(user.Id, Guid.NewGuid(), true, false, false, false);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await userPermissionsRepository.DidNotReceive().AddAsync(
            Arg.Any<UserPermissionEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_conflict_when_permission_already_exists_for_user_and_module()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        var module = new ModuleEntity("Usuarios", null);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        modulesRepository.GetByIdAsync(module.Id, Arg.Any<CancellationToken>()).Returns(module);
        userPermissionsRepository.GetByUserAndModuleIdAsync(user.Id, module.Id, Arg.Any<CancellationToken>())
            .Returns(new UserPermissionEntity(user.Id, module.Id, true, false, false, false));

        var command = new CreateUserPermissionCommand(user.Id, module.Id, true, true, false, false);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
        await userPermissionsRepository.DidNotReceive().AddAsync(
            Arg.Any<UserPermissionEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_creates_permission_when_user_and_module_exist_and_no_duplicate()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", RoleId);
        var module = new ModuleEntity("Usuarios", null);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        modulesRepository.GetByIdAsync(module.Id, Arg.Any<CancellationToken>()).Returns(module);
        userPermissionsRepository.GetByUserAndModuleIdAsync(user.Id, module.Id, Arg.Any<CancellationToken>())
            .Returns((UserPermissionEntity?)null);

        var command = new CreateUserPermissionCommand(user.Id, module.Id, true, true, true, true);

        var id = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await userPermissionsRepository.Received(1).AddAsync(
            Arg.Any<UserPermissionEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
