namespace Api.Reports.Dtos;

public sealed record AppointmentVeterinarianReportResponse(
    Guid VeterinarianId,
    string VeterinarianName,
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int ScheduledCount,
    int OtherCount);
