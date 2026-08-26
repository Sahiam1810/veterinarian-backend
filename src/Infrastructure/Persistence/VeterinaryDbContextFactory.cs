using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContextFactory : IDesignTimeDbContextFactory<VeterinaryDbContext>
{
    public VeterinaryDbContext CreateDbContext(string[] args)
    {
        Env.TraversePath().Load();

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle(connectionString)
            .Options;

        return new VeterinaryDbContext(options);
    }
}