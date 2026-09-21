using Infrastructure.Telegram.Configuration;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramSecurityTests
{
    [Fact]
    public void Disabled_options_allow_empty_external_configuration()
    {
        var result = new TelegramOptionsValidator().Validate(null, new TelegramOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Enabled_options_require_all_secrets_and_https_url()
    {
        var result = new TelegramOptionsValidator().Validate(
            null,
            new TelegramOptions { Enabled = true });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("BotToken"));
        Assert.Contains(result.Failures!, failure => failure.Contains("WebhookSecret"));
        Assert.Contains(result.Failures!, failure => failure.Contains("PublicWebhookUrl"));
    }
}
