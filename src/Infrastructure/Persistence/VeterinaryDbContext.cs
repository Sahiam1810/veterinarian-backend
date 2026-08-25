using HelpDesk.Infrastructure.Roles.Configuration;
using Microsoft.EntityFrameworkCore;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options)
    : DbContext(options)
{
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RolesConfiguration());
    }
}
