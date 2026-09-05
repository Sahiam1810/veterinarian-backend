using Application.Common.Abstractions;
using Application.Modules.Abstraction;
using Application.Permissions.UseCases;
using Application.RolePermissions.Abstraction;
using Application.UserPermissions.Abstraction;
using NSubstitute;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;
using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;
using Xunit;

namespace Application.Tests.Permissions;

public sealed class GetUserEffectivePermissionsQueryHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IModulesRepository modulesRepository = Substitute.For<IModulesRepository>();
    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUserPermissionsRepository userPermissionsRepository = Substitute.For<IUserPermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public GetUserEffectivePermissionsQueryHandlerTests()
    {
        unitOfWork.ModulesRepository.Returns(modulesRepository);
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
        unitOfWork.UserPermissionsRepository.Returns(userPermissionsRepository);
    }

    [Fact]
    public async Task Handle_combines_role_and_user_rows_with_three_bulk_reads()
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
        userPermissionsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([
                new UserPermissionEntity(
                    UserId,
                    clientsModule.Id,
                    canView: true,
                    canCreate: true,
                    canEdit: false,
                    canDelete: false)
            ]);

        var sut = new GetUserEffectivePermissionsQueryHandler(unitOfWork);

        var result = await sut.Handle(
            new GetUserEffectivePermissionsQuery(RoleId, UserId),
            CancellationToken.None);

        Assert.True(result["Mascotas"].CanView);
        Assert.True(result["Clientes"].CanCreate);
        await modulesRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await rolePermissionsRepository.Received(1)
            .GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>());
        await userPermissionsRepository.Received(1)
            .GetByUserIdAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_does_not_let_a_false_user_row_revoke_a_role_grant()
    {
        var module = new ModuleEntity("Citas", null);
        modulesRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([module]);
        rolePermissionsRepository.GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>())
            .Returns([
                new RolePermissionEntity(
                    RoleId,
                    module.Id,
                    canView: true,
                    canCreate: true,
                    canEdit: false,
                    canDelete: false)
            ]);
        userPermissionsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([
                new UserPermissionEntity(
                    UserId,
                    module.Id,
                    canView: false,
                    canCreate: false,
                    canEdit: false,
                    canDelete: false)
            ]);

        var sut = new GetUserEffectivePermissionsQueryHandler(unitOfWork);

        var result = await sut.Handle(
            new GetUserEffectivePermissionsQuery(RoleId, UserId),
            CancellationToken.None);

        Assert.True(result["Citas"].CanView);
        Assert.True(result["Citas"].CanCreate);
    }
}
