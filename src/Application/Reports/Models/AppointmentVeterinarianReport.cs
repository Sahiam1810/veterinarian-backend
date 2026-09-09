namespace Application.Reports.Models;

// Proyección mínima del repositorio: evita cargar dueños, mascotas u otras PII.
public sealed record AppointmentVeterinarianReportEntry(
    Guid VeterinarianId,
    string VeterinarianName,
    string StatusName);

public sealed record AppointmentVeterinarianReport(
    Guid VeterinarianId,
    string VeterinarianName,
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int ScheduledCount,
    int OtherCount);
