using Application.Telegram.Linking;
using Application.Telegram.Updates;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramCommandValidatorsTests
{
    [Fact]
    public async Task Create_link_code_rejects_empty_person_id()
    {
        var result = await new CreateTelegramLinkCodeCommandValidator()
            .ValidateAsync(new CreateTelegramLinkCodeCommand(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Consume_link_code_rejects_missing_code_and_external_ids()
    {
        var result = await new ConsumeTelegramLinkCodeCommandValidator()
            .ValidateAsync(new ConsumeTelegramLinkCodeCommand("", 0, 0));

        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public async Task Ingest_rejects_text_larger_than_telegram_limit()
    {
        var result = await new IngestTelegramUpdateCommandValidator()
            .ValidateAsync(new IngestTelegramUpdateCommand(
                1, 1, 1, 1, "private", new string('a', 4097)));

        Assert.Contains(result.Errors, error => error.PropertyName == "Text");
    }
}
