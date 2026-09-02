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

// Cubre el enriquecimiento agregado a UserPermissionDetail: las consultas ya
// no devuelven solo GUIDs, resuelven UserFullName/UserEmail/ModuleName.
public sealed class GetUserPermissionQueriesTests
{
    private static readonly Guid RoleId = Guid.NewGuid();

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IModulesRepository modulesRepository = Substitute.For<IModulesRepository>();
    private readonly IUserPermissionsRepository userPermissionsRepository = Substitute.For<IUserPermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetUserPermissionQueriesTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.ModulesRepository.Returns(modulesRepository);
        unitOfWork.UserPermissionsRepository.Returns(userPermissionsRepository);
    }

    [Fact]
    public async Task GetById_throws_not_found_when_permission_does_not_exist()
    {
        var sut = new GetUserPermissionByIdQueryHandler(unitOfWork);
        var id = Guid.NewGuid();
        userPermissionsRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((UserPermissionEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetUserPermissionByIdQuery(id), CancellationToken.None));
    }

    [Fact]
    public async Task GetById_resolves_user_and_module_details()
    {
        var user = new UserEntity("Ana Pérez", "ana@huellitas.test", "hash", RoleId);
        var module = new ModuleEntity("Roles", null);
        var permission = new UserPermissionEntity(user.Id, module.Id, true, false, true, false);
        userPermissionsRepository.GetByIdAsync(permission.Id, Arg.Any<CancellationToken>()).Returns(permission);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        modulesRepository.GetByIdAsync(module.Id, Arg.Any<CancellationToken>()).Returns(module);

        var sut = new GetUserPermissionByIdQueryHandler(unitOfWork);
        var result = await sut.Handle(new GetUserPermissionByIdQuery(permission.Id), CancellationToken.None);

        Assert.Equal("Ana Pérez", result.UserFullName);
        Assert.Equal("ana@huellitas.test", result.UserEmail);
        Assert.Equal("Roles", result.ModuleName);
        Assert.True(result.CanView);
        Assert.True(result.CanEdit);
    }

    [Fact]
    public async Task GetAll_resolves_details_for_every_row()
    {
        var user = new UserEntity("Beto Ruiz", "beto@huellitas.test", "hash", RoleId);
        var module = new ModuleEntity("Citas", null);
        var permission = new UserPermissionEntity(user.Id, module.Id, true, true, false, false);

        userPermissionsRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { permission });
        usersRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { user });
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { module });

        var sut = new GetAllUserPermissionsQueryHandler(unitOfWork);
        var result = await sut.Handle(new GetAllUserPermissionsQuery(), CancellationToken.None);

        var detail = Assert.Single(result);
        Assert.Equal("Beto Ruiz", detail.UserFullName);
        Assert.Equal("beto@huellitas.test", detail.UserEmail);
        Assert.Equal("Citas", detail.ModuleName);
    }

    [Fact]
    public async Task GetByUserId_resolves_user_details_once_and_module_name_per_row()
    {
        var user = new UserEntity("Carla Soto", "carla@huellitas.test", "hash", RoleId);
        var moduleA = new ModuleEntity("Clientes", null);
        var moduleB = new ModuleEntity("Mascotas", null);
        var permissionA = new UserPermissionEntity(user.Id, moduleA.Id, true, false, false, false);
        var permissionB = new UserPermissionEntity(user.Id, moduleB.Id, true, false, false, false);

        userPermissionsRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { permissionA, permissionB });
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { moduleA, moduleB });

        var sut = new GetUserPermissionsByUserIdQueryHandler(unitOfWork);
        var result = await sut.Handle(new GetUserPermissionsByUserIdQuery(user.Id), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, detail => Assert.Equal("Carla Soto", detail.UserFullName));
        Assert.Contains(result, detail => detail.ModuleName == "Clientes");
        Assert.Contains(result, detail => detail.ModuleName == "Mascotas");
    }
}
