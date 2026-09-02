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

    [Fact]
    public void Enabled_registration_rejects_insecure_url_and_missing_protection_key()
    {
        var options = new TelegramOptions
        {
            Enabled = true,
            BotToken = "token",
            BotUsername = "bot",
            WebhookSecret = "secret",
            PublicWebhookUrl = "https://telegram.example.test",
            OtpPepperBase64 = Convert.ToBase64String(new byte[32]),
            RegistrationEnabled = true,
            RegistrationCompletionUrl = "http://registration.example.test"
        };

        var result = new TelegramOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("RegistrationCompletionUrl"));
        Assert.Contains(result.Failures, failure => failure.Contains("RegistrationProtectionKeyBase64"));
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
