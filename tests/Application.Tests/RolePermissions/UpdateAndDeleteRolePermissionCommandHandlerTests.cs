using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.RolePermissions.Abstraction;
using Application.RolePermissions.UseCases;
using NSubstitute;
using Xunit;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Application.Tests.RolePermissions;

public sealed class UpdateRolePermissionCommandHandlerTests
{
    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateRolePermissionCommandHandler sut;

    public UpdateRolePermissionCommandHandlerTests()
    {
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
        sut = new UpdateRolePermissionCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_permission_does_not_exist()
    {
        var id = Guid.NewGuid();
        rolePermissionsRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((RolePermissionEntity?)null);

        var command = new UpdateRolePermissionCommand(id, true, true, true, true);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_updates_flags_when_permission_exists()
    {
        var permission = new RolePermissionEntity(Guid.NewGuid(), Guid.NewGuid(), false, false, false, false);
        rolePermissionsRepository.GetByIdAsync(permission.Id, Arg.Any<CancellationToken>()).Returns(permission);

        var command = new UpdateRolePermissionCommand(permission.Id, true, true, false, false);

        await sut.Handle(command, CancellationToken.None);

        Assert.True(permission.CanView);
        Assert.True(permission.CanCreate);
        Assert.False(permission.CanEdit);
        Assert.False(permission.CanDelete);
        await rolePermissionsRepository.Received(1).UpdateAsync(permission, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class DeleteRolePermissionCommandHandlerTests
{
    private readonly IRolePermissionsRepository rolePermissionsRepository = Substitute.For<IRolePermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteRolePermissionCommandHandler sut;

    public DeleteRolePermissionCommandHandlerTests()
    {
        unitOfWork.RolePermissionsRepository.Returns(rolePermissionsRepository);
        sut = new DeleteRolePermissionCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_permission_does_not_exist()
    {
        var id = Guid.NewGuid();
        rolePermissionsRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((RolePermissionEntity?)null);

        var command = new DeleteRolePermissionCommand(id);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_deletes_permission_when_it_exists()
    {
        var permission = new RolePermissionEntity(Guid.NewGuid(), Guid.NewGuid(), true, true, true, true);
        rolePermissionsRepository.GetByIdAsync(permission.Id, Arg.Any<CancellationToken>()).Returns(permission);

        await sut.Handle(new DeleteRolePermissionCommand(permission.Id), CancellationToken.None);

        await rolePermissionsRepository.Received(1).DeleteAsync(permission, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
