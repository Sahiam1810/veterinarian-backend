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

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Pending_escalation_status_id_must_be_a_non_empty_guid(string invalidId)
    {
        var options = ValidOptions(pendingEscalationStatusId: invalidId);

        var result = new TelegramOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Text_message_type_id_must_be_a_non_empty_guid(string invalidId)
    {
        var options = ValidOptions(textMessageTypeId: invalidId);

        var result = new TelegramOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    private static TelegramOptions ValidOptions(
        int absoluteHours = 24,
        int idleMinutes = 30,
        string pendingEscalationStatusId = "85000000-0000-0000-0000-000000000001",
        string textMessageTypeId = "83000000-0000-0000-0000-000000000001") => new()
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
        PrivateAccessIdleTtlMinutes = idleMinutes,
        PendingEscalationStatusId = pendingEscalationStatusId,
        TextMessageTypeId = textMessageTypeId
    };
}
