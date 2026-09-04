using System.Security.Cryptography;
using Infrastructure.Telegram.Configuration;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramOptionsValidatorTests
{
    [Fact]
    public void Valid_options_accept_private_access_defaults()
    {
        var options = ValidOptions();

        var result = new TelegramOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
        Assert.Equal(24, options.PrivateAccessAbsoluteTtlHours);
        Assert.Equal(30, options.PrivateAccessIdleTtlMinutes);
    }

    [Fact]
    public void Idle_access_cannot_outlive_absolute_access()
    {
        var options = ValidOptions(absoluteHours: 1, idleMinutes: 61);

        var result = new TelegramOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    private static TelegramOptions ValidOptions(
        int absoluteHours = 24,
        int idleMinutes = 30) => new()
    {
        Enabled = true,
        GuestModeEnabled = true,
        BotToken = "123:token",
        BotUsername = "huellitas_bot",
        WebhookSecret = "valid_secret",
        PublicWebhookUrl = "https://example.test",
        OtpPepperBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        RegistrationEnabled = true,
        RegistrationCompletionUrl = "https://example.test/register",
        RegistrationProtectionKeyBase64 =
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        PrivateAccessAbsoluteTtlHours = absoluteHours,
        PrivateAccessIdleTtlMinutes = idleMinutes
    };
}
