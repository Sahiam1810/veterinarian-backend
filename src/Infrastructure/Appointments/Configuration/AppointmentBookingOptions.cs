namespace Infrastructure.Appointments.Configuration;

public sealed class AppointmentBookingOptions
{
    public const string SectionName = "AppointmentBooking";

    public string TimeZoneId { get; init; } = "America/Bogota";
    public int MinimumLeadMinutes { get; init; } = 60;
    public int MaximumAdvanceDays { get; init; } = 30;
}
