using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.Roles.UseCase;
using Domain.Roles;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Application.Tests.Roles;

public sealed class SystemRoleProtectionTests
{
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    public SystemRoleProtectionTests()
    {
        unitOfWork.RolesRepository.Returns(rolesRepository);
    }

    [Fact]
    public async Task Create_rejects_the_reserved_SuperAdmin_name()
    {
        var handler = new CreateRoleCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new CreateRoleCommand(" superadmin ", "Suplantación"),
            CancellationToken.None));

        await rolesRepository.DidNotReceive().AddAsync(
            Arg.Any<RoleEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_rejects_the_canonical_SuperAdmin_role()
    {
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema"));
        var handler = new UpdateRoleCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new UpdateRoleCommand(SystemRoles.SuperAdminId, "Otro nombre", null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Delete_rejects_the_canonical_SuperAdmin_role()
    {
        rolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, Arg.Any<CancellationToken>())
            .Returns(new RoleEntity(SystemRoles.SuperAdminName, "Rol de sistema"));
        var handler = new DeleteRoleCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new DeleteRoleCommand(SystemRoles.SuperAdminId),
            CancellationToken.None));

        await rolesRepository.DidNotReceive().DeleteAsync(
            Arg.Any<RoleEntity>(),
            Arg.Any<CancellationToken>());
    }
}
