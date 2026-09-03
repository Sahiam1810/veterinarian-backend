using Application.Appointments.Abstraction;
using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.Configuration;

public sealed class ConfiguredAppointmentBookingSettings(
    IOptions<AppointmentBookingOptions> options) : IAppointmentBookingSettings
{
    private readonly AppointmentBookingOptions _options = options.Value;

    public string TimeZoneId => _options.TimeZoneId;
    public TimeSpan MinimumLeadTime => TimeSpan.FromMinutes(_options.MinimumLeadMinutes);
    public int MaximumAdvanceDays => _options.MaximumAdvanceDays;
}
