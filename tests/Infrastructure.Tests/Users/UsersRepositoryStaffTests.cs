using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;
using Infrastructure.Persistence;
using Infrastructure.Users.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Users;

public sealed class UsersRepositoryStaffTests
{
    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }

    [Fact]
    public async Task GetStaffUsersAsync_returns_only_active_vets_auxiliaries_and_admins_with_role_name()
    {
        await using var context = CreateContext();
        var vet = new RoleEntity("Veterinario", null);
        var aux = new RoleEntity("Auxiliar", null);
        var admin = new RoleEntity("Administrador", null);
        var recep = new RoleEntity("Recepcionista", null);
        context.Set<RoleEntity>().AddRange(vet, aux, admin, recep);

        var vetUser = new UserEntity("Dra. Ana", "ana@test.com", "hash", vet.Id);
        var auxUser = new UserEntity("Pedro Aux", "pedro@test.com", "hash", aux.Id);
        var adminUser = new UserEntity("Luz Admin", "luz@test.com", "hash", admin.Id);
        var recepUser = new UserEntity("Maria Recep", "maria@test.com", "hash", recep.Id);
        var inactiveVet = new UserEntity("Inactivo Vet", "inactivo@test.com", "hash", vet.Id);
        inactiveVet.Deactivate();
        context.Set<UserEntity>().AddRange(vetUser, auxUser, adminUser, recepUser, inactiveVet);
        await context.SaveChangesAsync();

        var staff = await new UsersRepository(context).GetStaffUsersAsync();

        Assert.Equal(3, staff.Count);
        Assert.Contains(staff, s => s.User.Id == vetUser.Id && s.RoleName == "Veterinario");
        Assert.Contains(staff, s => s.User.Id == auxUser.Id && s.RoleName == "Auxiliar");
        Assert.Contains(staff, s => s.User.Id == adminUser.Id && s.RoleName == "Administrador");
        Assert.DoesNotContain(staff, s => s.User.Id == recepUser.Id);
        Assert.DoesNotContain(staff, s => s.User.Id == inactiveVet.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_returns_only_requested_users_in_a_single_query()
    {
        await using var context = CreateContext();
        var role = new RoleEntity("Veterinario", null);
        context.Set<RoleEntity>().Add(role);
        var first = new UserEntity("Uno", "uno@test.com", "hash", role.Id);
        var second = new UserEntity("Dos", "dos@test.com", "hash", role.Id);
        var other = new UserEntity("Tres", "tres@test.com", "hash", role.Id);
        context.Set<UserEntity>().AddRange(first, second, other);
        await context.SaveChangesAsync();

        var result = await new UsersRepository(context).GetByIdsAsync(new[] { first.Id, second.Id, first.Id });

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, u => u.Id == other.Id);
        Assert.Empty(await new UsersRepository(context).GetByIdsAsync(Array.Empty<Guid>()));
    }
}
