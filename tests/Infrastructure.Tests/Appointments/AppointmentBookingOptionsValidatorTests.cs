using Infrastructure.Appointments.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Appointments;

public sealed class AppointmentBookingOptionsValidatorTests
{
    [Fact]
    public void Validate_accepts_supported_booking_configuration()
    {
        var options = new AppointmentBookingOptions
        {
            TimeZoneId = "America/Bogota",
            MinimumLeadMinutes = 60,
            MaximumAdvanceDays = 30,
        };

        var result = new AppointmentBookingOptionsValidator().Validate(null, options);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_rejects_invalid_limits_or_timezone()
    {
        var options = new AppointmentBookingOptions
        {
            TimeZoneId = "Invalid/Zone",
            MinimumLeadMinutes = -1,
            MaximumAdvanceDays = 0,
        };

        var result = new AppointmentBookingOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(3, result.Failures.Count());
    }
}
