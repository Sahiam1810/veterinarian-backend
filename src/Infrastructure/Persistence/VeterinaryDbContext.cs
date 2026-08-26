using Domain.Diagnostics.Entities;
using Domain.Pets.Entities;
using Microsoft.EntityFrameworkCore;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options)
    : DbContext(options)
{
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<PetEntity> Pets => Set<PetEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();

    public DbSet<UserCredentialsEntity> UserCredentials => Set<UserCredentialsEntity>();

    public DbSet<UserTokenEntity> UserTokens => Set<UserTokenEntity>();

    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeterinaryDbContext).Assembly);
    }
}
