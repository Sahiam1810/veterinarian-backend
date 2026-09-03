namespace Application.Appointments.Abstraction;

public interface IAppointmentBookingSettings
{
    string TimeZoneId { get; }

    TimeSpan MinimumLeadTime { get; }

    int MaximumAdvanceDays { get; }
}
