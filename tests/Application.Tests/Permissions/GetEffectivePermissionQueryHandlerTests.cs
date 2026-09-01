using Application.Common.Abstractions;
using Application.Permissions.UseCases;
using Application.RolePermissions.Abstraction;
using Application.UserPermissions.Abstraction;
using NSubstitute;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;
using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;
using Xunit;

namespace Application.Tests.Permissions;

public sealed class GetEffectivePermissionQueryHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string ModuleName = "Citas";

    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUserPermissionsRepository userPermissionsRepository = Substitute.For<IUserPermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetEffectivePermissionQueryHandler sut;

    public GetEffectivePermissionQueryHandlerTests()
    {
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
        unitOfWork.UserPermissionsRepository.Returns(userPermissionsRepository);
        sut = new GetEffectivePermissionQueryHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_returns_all_false_when_neither_role_nor_user_has_a_row()
    {
        RolePermissionRow(null);
        UserPermissionRow(null);

        var result = await sut.Handle(new GetEffectivePermissionQuery(RoleId, UserId, ModuleName), CancellationToken.None);

        Assert.False(result.CanView);
        Assert.False(result.CanCreate);
        Assert.False(result.CanEdit);
        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task Handle_matches_role_flags_exactly_when_only_the_role_has_a_row()
    {
        RolePermissionRow(new RolePermissionEntity(RoleId, Guid.NewGuid(), canView: true, canCreate: true, canEdit: false, canDelete: false));
        UserPermissionRow(null);

        var result = await sut.Handle(new GetEffectivePermissionQuery(RoleId, UserId, ModuleName), CancellationToken.None);

        Assert.True(result.CanView);
        Assert.True(result.CanCreate);
        Assert.False(result.CanEdit);
        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task Handle_matches_user_flags_exactly_when_only_the_user_has_a_row()
    {
        RolePermissionRow(null);
        UserPermissionRow(new UserPermissionEntity(UserId, Guid.NewGuid(), canView: false, canCreate: false, canEdit: true, canDelete: false));

        var result = await sut.Handle(new GetEffectivePermissionQuery(RoleId, UserId, ModuleName), CancellationToken.None);

        Assert.False(result.CanView);
        Assert.False(result.CanCreate);
        Assert.True(result.CanEdit);
        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task Handle_combines_role_and_user_permissions_additively_per_action()
    {
        // Rol: solo ver. Usuario: permiso puntual extra para crear.
        RolePermissionRow(new RolePermissionEntity(RoleId, Guid.NewGuid(), canView: true, canCreate: false, canEdit: false, canDelete: false));
        UserPermissionRow(new UserPermissionEntity(UserId, Guid.NewGuid(), canView: false, canCreate: true, canEdit: false, canDelete: false));

        var result = await sut.Handle(new GetEffectivePermissionQuery(RoleId, UserId, ModuleName), CancellationToken.None);

        Assert.True(result.CanView);
        Assert.True(result.CanCreate);
        Assert.False(result.CanEdit);
        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task Handle_never_lets_a_missing_user_override_take_away_what_the_role_grants()
    {
        RolePermissionRow(new RolePermissionEntity(RoleId, Guid.NewGuid(), canView: true, canCreate: true, canEdit: true, canDelete: true));
        UserPermissionRow(null);

        var result = await sut.Handle(new GetEffectivePermissionQuery(RoleId, UserId, ModuleName), CancellationToken.None);

        Assert.True(result.CanView);
        Assert.True(result.CanCreate);
        Assert.True(result.CanEdit);
        Assert.True(result.CanDelete);
    }

    private void RolePermissionRow(RolePermissionEntity? entity) =>
        rolePermissionsRepository
            .GetByRoleAndModuleNameAsync(RoleId, ModuleName, Arg.Any<CancellationToken>())
            .Returns(entity);

    private void UserPermissionRow(UserPermissionEntity? entity) =>
        userPermissionsRepository
            .GetByUserAndModuleNameAsync(UserId, ModuleName, Arg.Any<CancellationToken>())
            .Returns(entity);
}
