using Domain.Diagnostics.Entities;
using Microsoft.EntityFrameworkCore;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options)
    : DbContext(options)
{
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();

    public DbSet<UserCredentialsEntity> UserCredentials => Set<UserCredentialsEntity>();

    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeterinaryDbContext).Assembly);
    }
}
