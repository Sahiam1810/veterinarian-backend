using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.Configuration;

public sealed class TelegramReminderOptionsValidator : IValidateOptions<TelegramReminderOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramReminderOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.LeadMinutes < 1 || options.LeadMinutes > 1440)
        {
            failures.Add("TelegramReminders:LeadMinutes must be between 1 and 1440.");
        }

        if (options.GraceMinutes < 0 || options.GraceMinutes > options.LeadMinutes)
        {
            failures.Add("TelegramReminders:GraceMinutes must be between 0 and LeadMinutes.");
        }

        if (options.PollIntervalMinutes < 1 || options.PollIntervalMinutes > 1440)
        {
            failures.Add("TelegramReminders:PollIntervalMinutes must be between 1 and 1440.");
        }

        if (options.AllowedStatusNames is null ||
            options.AllowedStatusNames.Length == 0 ||
            options.AllowedStatusNames.All(string.IsNullOrWhiteSpace))
        {
            failures.Add("TelegramReminders:AllowedStatusNames must contain at least one status.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
