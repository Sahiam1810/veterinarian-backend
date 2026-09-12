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
        var registrationSession = context.Model.FindEntityType(typeof(TelegramRegistrationSession));
        var identitySession = context.Model.FindEntityType(typeof(TelegramIdentitySession));

        Assert.Equal("TELEGRAM_INBOUND_UPDATES", update.GetTableName());
        Assert.Equal("NUMBER(19)", update.FindProperty(nameof(TelegramInboundUpdate.TelegramChatId))!.GetColumnType());
        Assert.Equal("TELEGRAM_USER_LINKS", userLink.GetTableName());
        var lifecycleScopedProperties = new[]
        {
            nameof(TelegramUserLink.PersonId),
            nameof(TelegramUserLink.TelegramUserId),
            nameof(TelegramUserLink.TelegramChatId)
        };
        Assert.DoesNotContain(userLink.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            lifecycleScopedProperties.Contains(index.Properties[0].Name));
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
        Assert.NotNull(registrationSession);
        Assert.Equal("TELEGRAM_REGISTRATION_SESSIONS", registrationSession.GetTableName());
        Assert.Equal(
            "VARCHAR2(64)",
            registrationSession.FindProperty(
                nameof(TelegramRegistrationSession.CompletionTokenHash))!.GetColumnType());
        Assert.Contains(registrationSession.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name ==
            nameof(TelegramRegistrationSession.CompletionTokenHash));
        Assert.NotNull(identitySession);
        Assert.Equal("TELEGRAM_IDENTITY_SESSIONS", identitySession.GetTableName());
        Assert.Equal(
            "NUMBER(19)",
            identitySession.FindProperty(
                nameof(TelegramIdentitySession.PendingInboundUpdateId))!.GetColumnType());
        Assert.Equal(
            "CLOB",
            identitySession.FindProperty(
                nameof(TelegramIdentitySession.ProtectedPendingMessage))!.GetColumnType());
        Assert.Contains(identitySession.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Single().Name ==
            nameof(TelegramIdentitySession.PendingInboundUpdateId));
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
