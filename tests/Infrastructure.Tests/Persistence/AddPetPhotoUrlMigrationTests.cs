using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Infrastructure.Tests.Persistence;

public sealed class AddPetPhotoUrlMigrationTests
{
    [Fact]
    public void MigrationAssembly_DiscoversAddPetPhotoUrl()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=test;Password=test;Data Source=test")
            .Options;
        using var context = new VeterinaryDbContext(options);

        var migrations = context.GetService<IMigrationsAssembly>().Migrations;

        Assert.Contains("20260908150000_AddPetPhotoUrl", migrations.Keys);
    }
}
