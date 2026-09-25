using Application.Common.Abstractions;
using Application.Modules.Abstraction;
using Application.Permissions.UseCases;
using Application.RolePermissions.Abstraction;
using NSubstitute;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;
using Xunit;

namespace Application.Tests.Permissions;

public sealed class GetUserEffectivePermissionsQueryHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IModulesRepository modulesRepository = Substitute.For<IModulesRepository>();
    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetUserEffectivePermissionsQueryHandlerTests()
    {
        unitOfWork.ModulesRepository.Returns(modulesRepository);
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
    }

    [Fact]
    public async Task Handle_returns_the_role_flags_for_every_module()
    {
        var clientsModule = new ModuleEntity("Clientes", null);
        var petsModule = new ModuleEntity("Mascotas", null);
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([clientsModule, petsModule]);
        rolePermissionsRepository.GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>())
            .Returns([
                new RolePermissionEntity(
                    RoleId,
                    petsModule.Id,
                    canView: true,
                    canCreate: false,
                    canEdit: false,
                    canDelete: false)
            ]);

        var sut = new GetUserEffectivePermissionsQueryHandler(unitOfWork);

        var result = await sut.Handle(
            new GetUserEffectivePermissionsQuery(RoleId),
            CancellationToken.None);

        Assert.True(result["Mascotas"].CanView);
        Assert.False(result["Clientes"].CanView);
        await modulesRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await rolePermissionsRepository.Received(1)
            .GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_all_false_for_a_module_without_a_role_row()
    {
        var module = new ModuleEntity("Citas", null);
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([module]);
        rolePermissionsRepository.GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RolePermissionEntity>());

        var sut = new GetUserEffectivePermissionsQueryHandler(unitOfWork);

        var result = await sut.Handle(
            new GetUserEffectivePermissionsQuery(RoleId),
            CancellationToken.None);

        Assert.False(result["Citas"].CanView);
        Assert.False(result["Citas"].CanCreate);
        Assert.False(result["Citas"].CanEdit);
        Assert.False(result["Citas"].CanDelete);
    }
}
