using Infrastructure.Appointments.Configuration;
using Xunit;

namespace Infrastructure.Tests.Appointments;

public sealed class TelegramReminderOptionsValidatorTests
{
    [Fact]
    public void Disabled_options_skip_validation()
    {
        var options = new TelegramReminderOptions { Enabled = false, LeadMinutes = 0 };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Default_values_are_valid()
    {
        var result = new TelegramReminderOptionsValidator().Validate(null, new TelegramReminderOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Lead_must_be_between_1_and_1440()
    {
        var options = new TelegramReminderOptions { LeadMinutes = 0 };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Grace_cannot_exceed_lead()
    {
        var options = new TelegramReminderOptions { LeadMinutes = 60, GraceMinutes = 61 };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Allowed_statuses_are_required_when_enabled()
    {
        var options = new TelegramReminderOptions { AllowedStatusNames = [] };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }
}
