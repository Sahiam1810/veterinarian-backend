namespace Application.Reports.Models;

public sealed record AppointmentsSummaryReport(
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int NoShowCount,
    int ScheduledCount,
    decimal AttendanceRate,
    Guid? TopServiceId,
    string? TopServiceName,
    int TopServiceCount,
    decimal TopServicePercentage);
