namespace Api.Reports.Dtos;

public sealed record AppointmentsByDayReportResponse(
    DateOnly Date,
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int ScheduledCount);
