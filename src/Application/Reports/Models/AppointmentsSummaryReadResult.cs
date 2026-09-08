namespace Application.Reports.Models;

public sealed record AppointmentsSummaryReadResult(
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int NoShowCount,
    int ScheduledCount,
    ServiceAppointmentCount? TopService);
