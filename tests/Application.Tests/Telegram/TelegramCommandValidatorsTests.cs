using Application.Telegram.Updates;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramCommandValidatorsTests
{
    [Fact]
    public async Task Ingest_rejects_text_larger_than_telegram_limit()
    {
        var result = await new IngestTelegramUpdateCommandValidator()
            .ValidateAsync(new IngestTelegramUpdateCommand(
                1, 1, 1, 1, "private", new string('a', 4097)));

        Assert.Contains(result.Errors, error => error.PropertyName == "Text");
    }
}
