using System.Reflection;
using System.Runtime.CompilerServices;
using Domain.Species.Entities;
using Infrastructure.Persistence;
using InfrastructureUnitOfWork = Infrastructure.UnitOfWork.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Transactions;

public sealed class UnitOfWorkTransactionTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBack_InsertedRows_When_Action_Fails()
    {
        await using var context = CreateContext();
        var unitOfWork = (InfrastructureUnitOfWork)RuntimeHelpers.GetUninitializedObject(typeof(InfrastructureUnitOfWork));
        typeof(InfrastructureUnitOfWork)
            .GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(unitOfWork, context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.ExecuteInTransactionAsync(_ =>
            {
                context.Species.Add(new SpeciesEntity("Temporal"));
                throw new InvalidOperationException("fallo intencional");
            }));

        Assert.Empty(await context.Species.AsNoTracking().ToListAsync());
    }

    private VeterinaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseSqlite(connection)
            .Options);
}
