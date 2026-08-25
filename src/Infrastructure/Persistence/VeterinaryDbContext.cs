
using HelpDesk.Infrastructure.Roles.Configuration;
using Domain.Diagnostics.Entities;
using Microsoft.EntityFrameworkCore;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options)
    : DbContext(options)
{
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfiguration(new RolesConfiguration());

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeterinaryDbContext).Assembly);

    }
}
