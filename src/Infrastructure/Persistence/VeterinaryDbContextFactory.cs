using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContextFactory : IDesignTimeDbContextFactory<VeterinaryDbContext>
{
    public VeterinaryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=design_time;Password=design_time;Data Source=design_time")
            .Options;

        return new VeterinaryDbContext(options);
    }
}
