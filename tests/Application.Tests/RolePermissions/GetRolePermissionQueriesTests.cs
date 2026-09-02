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

// Cubre el enriquecimiento agregado a RolePermissionDetail: las consultas ya
// no devuelven solo GUIDs, resuelven RoleName/ModuleName contra Roles/Modules.
public sealed class GetRolePermissionQueriesTests
{
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IModulesRepository modulesRepository = Substitute.For<IModulesRepository>();
    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetRolePermissionQueriesTests()
    {
        unitOfWork.RolesRepository.Returns(rolesRepository);
        unitOfWork.ModulesRepository.Returns(modulesRepository);
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
    }

    [Fact]
    public async Task GetById_throws_not_found_when_permission_does_not_exist()
    {
        var sut = new GetRolePermissionByIdQueryHandler(unitOfWork);
        var id = Guid.NewGuid();
        rolePermissionsRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((RolePermissionEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetRolePermissionByIdQuery(id), CancellationToken.None));
    }

    [Fact]
    public async Task GetById_resolves_role_and_module_names()
    {
        var role = new RoleEntity("Administrador", null);
        var module = new ModuleEntity("Usuarios", null);
        var permission = new RolePermissionEntity(role.Id, module.Id, true, false, true, false);
        rolePermissionsRepository.GetByIdAsync(permission.Id, Arg.Any<CancellationToken>()).Returns(permission);
        rolesRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        modulesRepository.GetByIdAsync(module.Id, Arg.Any<CancellationToken>()).Returns(module);

        var sut = new GetRolePermissionByIdQueryHandler(unitOfWork);
        var result = await sut.Handle(new GetRolePermissionByIdQuery(permission.Id), CancellationToken.None);

        Assert.Equal("Administrador", result.RoleName);
        Assert.Equal("Usuarios", result.ModuleName);
        Assert.True(result.CanView);
        Assert.True(result.CanEdit);
    }

    [Fact]
    public async Task GetAll_resolves_names_for_every_row()
    {
        var role = new RoleEntity("Veterinario", null);
        var module = new ModuleEntity("Citas", null);
        var permission = new RolePermissionEntity(role.Id, module.Id, true, true, false, false);

        rolePermissionsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { permission });
        rolesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { role });
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { module });

        var sut = new GetAllRolePermissionsQueryHandler(unitOfWork);
        var result = await sut.Handle(new GetAllRolePermissionsQuery(), CancellationToken.None);

        var detail = Assert.Single(result);
        Assert.Equal("Veterinario", detail.RoleName);
        Assert.Equal("Citas", detail.ModuleName);
    }

    [Fact]
    public async Task GetByRoleId_resolves_role_name_once_and_module_name_per_row()
    {
        var role = new RoleEntity("Recepcionista", null);
        var moduleA = new ModuleEntity("Clientes", null);
        var moduleB = new ModuleEntity("Mascotas", null);
        var permissionA = new RolePermissionEntity(role.Id, moduleA.Id, true, false, false, false);
        var permissionB = new RolePermissionEntity(role.Id, moduleB.Id, true, false, false, false);

        rolePermissionsRepository.GetByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { permissionA, permissionB });
        rolesRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { moduleA, moduleB });

        var sut = new GetRolePermissionsByRoleIdQueryHandler(unitOfWork);
        var result = await sut.Handle(new GetRolePermissionsByRoleIdQuery(role.Id), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, detail => Assert.Equal("Recepcionista", detail.RoleName));
        Assert.Contains(result, detail => detail.ModuleName == "Clientes");
        Assert.Contains(result, detail => detail.ModuleName == "Mascotas");
    }
}
