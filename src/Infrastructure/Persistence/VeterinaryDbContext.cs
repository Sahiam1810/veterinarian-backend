using Domain.Diagnostics.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options) : DbContext(options)
{
    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeterinaryDbContext).Assembly);
    }
}
