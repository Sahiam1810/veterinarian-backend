using Infrastructure.Telegram.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests.Telegram;

public sealed class TelegramOptionsStartupTests
{
    [Fact]
    public void Disabled_channel_does_not_require_external_secrets()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Telegram:Enabled"] = "false"
        });

        Assert.False(provider.GetRequiredService<IOptions<TelegramOptions>>().Value.Enabled);
    }

    [Fact]
    public void Enabled_channel_rejects_incomplete_external_configuration()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Telegram:Enabled"] = "true"
        });

        var exception = Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<TelegramOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains("Telegram:BotToken"));
        Assert.Contains(exception.Failures, failure => failure.Contains("Telegram:WebhookSecret"));
        Assert.Contains(exception.Failures, failure => failure.Contains("Telegram:PublicWebhookUrl"));
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IValidateOptions<TelegramOptions>, TelegramOptionsValidator>();
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateOnStart();
        return services.BuildServiceProvider();
    }
}
