using Domain.Clients.Entities;
using Infrastructure.Clients.Repositories;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Clients;

// P0: un PHONE_NUMBER histórico inválido no debe tumbar GetAllAsync al materializar.
public sealed class ClientPhoneHistoricalReadToleranceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task GetAllAsync_materializes_invalid_historical_phone_as_null_without_dropping_rows()
    {
        await using var context = CreateContext();
        var validId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var invalidId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Bypass del converter: inserta el string crudo como en Oracle histórico.
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "CLIENTS" (
                "CLIENT_ID", "FULL_NAME", "EMAIL", "IS_ACTIVE",
                "IDENTIFICATION_NUMBER", "ADDRESS", "PHONE_NUMBER", "CREATED_AT")
            VALUES
                ({0}, 'Cliente Valido', 'valido@test.local', 1,
                 '1000000001', NULL, '3001234567', {1}),
                ({2}, 'Cliente Historico', 'historico@test.local', 1,
                 '1000000002', NULL, '123', {3});
            """,
            validId.ToString(),
            DateTime.UtcNow,
            invalidId.ToString(),
            DateTime.UtcNow);

        var repository = new ClientRepository(context);

        var clients = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, clients.Count);
        var valid = Assert.Single(clients, c => c.Id == validId);
        var invalid = Assert.Single(clients, c => c.Id == invalidId);
        Assert.Equal("3001234567", valid.PhoneNumber!.Value);
        Assert.Null(invalid.PhoneNumber);
    }

    private VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new VeterinaryDbContext(options);
    }
}
