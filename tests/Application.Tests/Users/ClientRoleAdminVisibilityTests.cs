using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Roles.Abstraction;
using Application.Roles.UseCase;
using Application.Users.Abstraction;
using Application.Users.UseCase;
using Domain.Common;
using Domain.Roles;
using NSubstitute;
using Xunit;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Users;

public sealed class ClientRoleAdminVisibilityTests
{
    private static readonly string[] StaffRoleNames =
        ["SuperAdmin", "Administrador", "Veterinario", "Recepcionista", "Auxiliar"];

    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository rolesRepository = Substitute.For<IRolesRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();

    public ClientRoleAdminVisibilityTests()
    {
        unitOfWork.UsersRepository.Returns(usersRepository);
        unitOfWork.RolesRepository.Returns(rolesRepository);
    }

    [Fact]
    public async Task GetAllRoles_hides_the_Cliente_role_and_keeps_the_staff_roles()
    {
        var staffRoles = StaffRoleNames
            .Select(name => name == SystemRoles.SuperAdminName
                ? WithId(new RoleEntity(name, null), SystemRoles.SuperAdminId)
                : new RoleEntity(name, null))
            .ToArray();
        var clientRole = WithId(new RoleEntity("Cliente", null), SystemRoles.ClientRoleId);
        rolesRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(staffRoles.Append(clientRole).ToArray());

        var result = await new GetAllRolesQueryHandler(unitOfWork)
            .Handle(new GetAllRolesQuery(), CancellationToken.None);

        Assert.DoesNotContain(result, role => role.Id == SystemRoles.ClientRoleId);
        Assert.Equal(StaffRoleNames, result.Select(role => role.Name.Value));
    }

    [Fact]
    public async Task GetAllRoles_hides_Cliente_by_id_even_if_it_was_renamed()
    {
        var renamedClientRole = WithId(new RoleEntity("Portal", null), SystemRoles.ClientRoleId);
        rolesRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new RoleEntity("Administrador", null), renamedClientRole });

        var result = await new GetAllRolesQueryHandler(unitOfWork)
            .Handle(new GetAllRolesQuery(), CancellationToken.None);

        Assert.Equal(new[] { "Administrador" }, result.Select(role => role.Name.Value));
    }

    [Fact]
    public async Task GetAllUsers_hides_users_with_the_Cliente_role()
    {
        var staffUser = new UserEntity("Ana", "ana@huellitas.test", "hash", Guid.NewGuid());
        var superAdmin = new UserEntity("Root", "root@huellitas.test", "hash", SystemRoles.SuperAdminId);
        var clientUser = new UserEntity("Portal", "portal@huellitas.test", "hash", SystemRoles.ClientRoleId);
        usersRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { staffUser, clientUser, superAdmin });

        var result = await new GetAllUsersQueryHandler(unitOfWork)
            .Handle(new GetAllUsersQuery(), CancellationToken.None);

        Assert.Equal(new[] { staffUser.Id, superAdmin.Id }, result.Select(user => user.Id));
    }

    [Fact]
    public async Task Create_rejects_assignment_of_the_Cliente_role()
    {
        var handler = new CreateUserCommandHandler(unitOfWork, passwordHasher);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(
            new CreateUserCommand(
                "Portal",
                "portal@huellitas.test",
                "ValidPassword!1",
                SystemRoles.ClientRoleId),
            CancellationToken.None));

        await usersRepository.DidNotReceive().AddAsync(
            Arg.Any<UserEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_rejects_assignment_of_the_Cliente_role()
    {
        var user = new UserEntity("Ana", "ana@huellitas.test", "hash", Guid.NewGuid());
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var originalRoleId = user.RoleId;
        var handler = new UpdateUserCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(
            new UpdateUserCommand(user.Id, user.FullName, user.Email.Value, SystemRoles.ClientRoleId),
            CancellationToken.None));

        Assert.Equal(originalRoleId, user.RoleId);
        await usersRepository.DidNotReceive().UpdateAsync(
            Arg.Any<UserEntity>(),
            Arg.Any<CancellationToken>());
    }

    private static TEntity WithId<TEntity>(TEntity entity, Guid id)
        where TEntity : BaseEntity<Guid>
    {
        typeof(BaseEntity<Guid>).GetProperty(nameof(BaseEntity<Guid>.Id))!.SetValue(entity, id);
        return entity;
    }
}
