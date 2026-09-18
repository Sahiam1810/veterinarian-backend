using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Infrastructure.Persistence;
using Infrastructure.Telegram.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramInboundUpdateClaimNextTests : IAsyncLifetime
{
    private static readonly DateTime Now =
        new(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task Claim_next_skips_pending_when_same_chat_is_already_processing()
    {
        await using var context = CreateContext();
        var repository = new TelegramInboundUpdateRepository(context);
        var busy = TelegramInboundUpdate.Create(1, 100, 100, 1, "private", "uno", Now);
        busy.Claim(Now);
        var waiting = TelegramInboundUpdate.Create(2, 100, 100, 2, "private", "dos", Now.AddSeconds(1));
        var otherChat = TelegramInboundUpdate.Create(3, 200, 200, 1, "private", "otro", Now.AddSeconds(2));
        await repository.AddAsync(busy, default);
        await repository.AddAsync(waiting, default);
        await repository.AddAsync(otherChat, default);
        await context.SaveChangesAsync();

        var claimed = await repository.ClaimNextAsync(Now.AddSeconds(3), Now.AddMinutes(-5), default);

        Assert.NotNull(claimed);
        Assert.Equal(3, claimed.Id);
        Assert.Equal(
            TelegramInboundUpdateStatus.Pending,
            (await repository.GetByIdAsync(2, default))!.Status);
    }

    [Fact]
    public async Task Claim_next_can_reclaim_stale_processing_for_same_chat()
    {
        await using var context = CreateContext();
        var repository = new TelegramInboundUpdateRepository(context);
        var stale = TelegramInboundUpdate.Create(10, 100, 100, 1, "private", "viejo", Now.AddMinutes(-10));
        stale.Claim(Now.AddMinutes(-10));
        await repository.AddAsync(stale, default);
        await context.SaveChangesAsync();

        var claimed = await repository.ClaimNextAsync(Now, Now.AddMinutes(-5), default);

        Assert.NotNull(claimed);
        Assert.Equal(10, claimed.Id);
        Assert.Equal(2, claimed.Attempts);
    }

    private VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new VeterinaryDbContext(options);
    }
}
