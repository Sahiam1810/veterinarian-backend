namespace Application.Reports.Models;

public sealed record ServiceAppointmentCount(
    Guid ServiceId,
    string ServiceName,
    int AppointmentsCount);
