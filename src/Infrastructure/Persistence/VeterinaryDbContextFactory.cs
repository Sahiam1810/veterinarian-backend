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
            .UseOracle(connectionString, oracle =>
                // XE 21c no soporta booleanos nativos (default del provider 23).
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21))
            .Options;

        return new VeterinaryDbContext(options);
    }
}