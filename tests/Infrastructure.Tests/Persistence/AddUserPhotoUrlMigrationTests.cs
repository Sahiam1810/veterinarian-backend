using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Infrastructure.Tests.Persistence;

public sealed class AddUserPhotoUrlMigrationTests
{
    [Fact]
    public void MigrationAssembly_DiscoversAddUserPhotoUrl()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=test;Password=test;Data Source=test")
            .Options;
        using var context = new VeterinaryDbContext(options);

        var migrations = context.GetService<IMigrationsAssembly>().Migrations;

        Assert.Contains("20260915180000_AddUserPhotoUrl", migrations.Keys);
    }
}
