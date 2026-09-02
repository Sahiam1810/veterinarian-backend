using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Modules.Abstraction;
using Application.RolePermissions.Abstraction;
using Application.RolePermissions.UseCases;
using Application.Roles.Abstraction;
using NSubstitute;
using Xunit;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using RoleEntity = Domain.Roles.Entities.Roles;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Application.Tests.RolePermissions;

public sealed class CreateRolePermissionCommandHandlerTests
{
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IModulesRepository modulesRepository = Substitute.For<IModulesRepository>();
    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateRolePermissionCommandHandler sut;

    public CreateRolePermissionCommandHandlerTests()
    {
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.ModulesRepository.Returns(modulesRepository);
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
        sut = new CreateRolePermissionCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_role_does_not_exist()
    {
        var moduleId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        rolesRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns((RoleEntity?)null);

        var command = new CreateRolePermissionCommand(roleId, moduleId, true, false, false, false);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await rolePermissionsRepository.DidNotReceive().AddAsync(
            Arg.Any<RolePermissionEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_not_found_when_module_does_not_exist()
    {
        var role = new RoleEntity("Administrador", null);
        rolesRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        modulesRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ModuleEntity?)null);

        var command = new CreateRolePermissionCommand(role.Id, Guid.NewGuid(), true, false, false, false);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await rolePermissionsRepository.DidNotReceive().AddAsync(
            Arg.Any<RolePermissionEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_conflict_when_permission_already_exists_for_role_and_module()
    {
        var role = new RoleEntity("Administrador", null);
        var module = new ModuleEntity("Usuarios", null);
        rolesRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        modulesRepository.GetByIdAsync(module.Id, Arg.Any<CancellationToken>()).Returns(module);
        rolePermissionsRepository.GetByRoleAndModuleIdAsync(role.Id, module.Id, Arg.Any<CancellationToken>())
            .Returns(new RolePermissionEntity(role.Id, module.Id, true, false, false, false));

        var command = new CreateRolePermissionCommand(role.Id, module.Id, true, true, false, false);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
        await rolePermissionsRepository.DidNotReceive().AddAsync(
            Arg.Any<RolePermissionEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_creates_permission_when_role_and_module_exist_and_no_duplicate()
    {
        var role = new RoleEntity("Administrador", null);
        var module = new ModuleEntity("Usuarios", null);
        rolesRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        modulesRepository.GetByIdAsync(module.Id, Arg.Any<CancellationToken>()).Returns(module);
        rolePermissionsRepository.GetByRoleAndModuleIdAsync(role.Id, module.Id, Arg.Any<CancellationToken>())
            .Returns((RolePermissionEntity?)null);

        var command = new CreateRolePermissionCommand(role.Id, module.Id, true, true, true, true);

        var id = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await rolePermissionsRepository.Received(1).AddAsync(
            Arg.Any<RolePermissionEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
