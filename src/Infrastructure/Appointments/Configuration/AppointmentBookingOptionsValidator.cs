using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.Configuration;

public sealed class AppointmentBookingOptionsValidator
    : IValidateOptions<AppointmentBookingOptions>
{
    public ValidateOptionsResult Validate(string? name, AppointmentBookingOptions options)
    {
        var failures = new List<string>();

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            failures.Add("AppointmentBooking:TimeZoneId is not a supported time zone.");
        }
        catch (InvalidTimeZoneException)
        {
            failures.Add("AppointmentBooking:TimeZoneId is invalid.");
        }

        if (options.MinimumLeadMinutes < 0 || options.MinimumLeadMinutes > 10080)
        {
            failures.Add("AppointmentBooking:MinimumLeadMinutes must be between 0 and 10080.");
        }

        if (options.MaximumAdvanceDays < 1 || options.MaximumAdvanceDays > 365)
        {
            failures.Add("AppointmentBooking:MaximumAdvanceDays must be between 1 and 365.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
