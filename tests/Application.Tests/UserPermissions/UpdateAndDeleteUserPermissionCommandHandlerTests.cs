using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserPermissions.Abstraction;
using Application.UserPermissions.UseCases;
using NSubstitute;
using Xunit;
using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;

namespace Application.Tests.UserPermissions;

public sealed class UpdateUserPermissionCommandHandlerTests
{
    private readonly IUserPermissionsRepository userPermissionsRepository = Substitute.For<IUserPermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserPermissionCommandHandler sut;

    public UpdateUserPermissionCommandHandlerTests()
    {
        unitOfWork.UserPermissionsRepository.Returns(userPermissionsRepository);
        sut = new UpdateUserPermissionCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_permission_does_not_exist()
    {
        var id = Guid.NewGuid();
        userPermissionsRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((UserPermissionEntity?)null);

        var command = new UpdateUserPermissionCommand(id, true, true, true, true);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_updates_flags_when_permission_exists()
    {
        var permission = new UserPermissionEntity(Guid.NewGuid(), Guid.NewGuid(), false, false, false, false);
        userPermissionsRepository.GetByIdAsync(permission.Id, Arg.Any<CancellationToken>()).Returns(permission);

        var command = new UpdateUserPermissionCommand(permission.Id, false, true, true, false);

        await sut.Handle(command, CancellationToken.None);

        Assert.False(permission.CanView);
        Assert.True(permission.CanCreate);
        Assert.True(permission.CanEdit);
        Assert.False(permission.CanDelete);
        await userPermissionsRepository.Received(1).UpdateAsync(permission, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class DeleteUserPermissionCommandHandlerTests
{
    private readonly IUserPermissionsRepository userPermissionsRepository = Substitute.For<IUserPermissionsRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteUserPermissionCommandHandler sut;

    public DeleteUserPermissionCommandHandlerTests()
    {
        unitOfWork.UserPermissionsRepository.Returns(userPermissionsRepository);
        sut = new DeleteUserPermissionCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_permission_does_not_exist()
    {
        var id = Guid.NewGuid();
        userPermissionsRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((UserPermissionEntity?)null);

        var command = new DeleteUserPermissionCommand(id);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_deletes_permission_when_it_exists()
    {
        var permission = new UserPermissionEntity(Guid.NewGuid(), Guid.NewGuid(), true, true, true, true);
        userPermissionsRepository.GetByIdAsync(permission.Id, Arg.Any<CancellationToken>()).Returns(permission);

        await sut.Handle(new DeleteUserPermissionCommand(permission.Id), CancellationToken.None);

        await userPermissionsRepository.Received(1).DeleteAsync(permission, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
