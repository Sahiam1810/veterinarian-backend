using Domain.Telegram.Entities;
using Infrastructure.Persistence;
using Infrastructure.Telegram.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramPersistenceTests
{
    private static readonly DateTime Now =
        new(2026, 8, 31, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Model_uses_approved_tables_and_external_id_types()
    {
        using var context = CreateOracleModelContext();

        var update = context.Model.FindEntityType(typeof(TelegramInboundUpdate))!;
        var userLink = context.Model.FindEntityType(typeof(TelegramUserLink))!;
        var linkingSession = context.Model.FindEntityType(typeof(TelegramLinkingSession));

        Assert.Equal("TELEGRAM_INBOUND_UPDATES", update.GetTableName());
        Assert.Equal("NUMBER(19)", update.FindProperty(nameof(TelegramInboundUpdate.TelegramChatId))!.GetColumnType());
        Assert.Equal("TELEGRAM_USER_LINKS", userLink.GetTableName());
        Assert.Contains(userLink.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name == nameof(TelegramUserLink.TelegramUserId));
        Assert.NotNull(linkingSession);
        Assert.Equal("TELEGRAM_LINKING_SESSIONS", linkingSession.GetTableName());
        Assert.Equal(
            "NUMBER(19)",
            linkingSession.FindProperty(nameof(TelegramLinkingSession.TelegramUserId))!.GetColumnType());
        Assert.Equal(
            "VARCHAR2(64)",
            linkingSession.FindProperty(nameof(TelegramLinkingSession.OtpHash))!.GetColumnType());
        Assert.Contains(linkingSession.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(TelegramLinkingSession.TelegramUserId), nameof(TelegramLinkingSession.Status)]));
    }

    [Fact]
    public async Task Inbound_repository_detects_existing_update()
    {
        await using var context = CreateContext();
        var repository = new TelegramInboundUpdateRepository(context);
        await repository.AddAsync(
            TelegramInboundUpdate.Create(42, 1001, 1001, 7, "private", "hola", Now),
            default);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsAsync(42, default);

        Assert.True(exists);
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }

    private static VeterinaryDbContext CreateOracleModelContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseOracle("User Id=unused;Password=unused;Data Source=unused")
            .Options;
        return new VeterinaryDbContext(options);
    }
}
