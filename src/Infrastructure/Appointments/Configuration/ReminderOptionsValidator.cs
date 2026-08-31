using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.Configuration;

public sealed class ReminderOptionsValidator : IValidateOptions<ReminderOptions>
{
    public ValidateOptionsResult Validate(string? name, ReminderOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.WindowHours <= 0 || options.WindowHours > 168)
        {
            failures.Add("Reminders:WindowHours must be between 0 (exclusive) and 168.");
        }

        if (options.PollIntervalMinutes < 1 || options.PollIntervalMinutes > 1440)
        {
            failures.Add("Reminders:PollIntervalMinutes must be between 1 and 1440.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
