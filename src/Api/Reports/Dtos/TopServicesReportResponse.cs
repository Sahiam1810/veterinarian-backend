namespace Api.Reports.Dtos;

public sealed record TopServiceReportResponse(
    Guid ServiceId,
    string ServiceName,
    int AppointmentsCount,
    decimal Percentage);

public sealed record AppointmentsSummaryReportResponse(
    DateOnly From,
    DateOnly To,
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int NoShowCount,
    int ScheduledCount,
    decimal AttendanceRate,
    string? TopServiceName,
    int TopServiceCount,
    decimal TopServicePercentage);
