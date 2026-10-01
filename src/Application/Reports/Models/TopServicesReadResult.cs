namespace Application.Reports.Models;

public sealed record TopServicesReadResult(
    int TotalAppointments,
    IReadOnlyList<ServiceAppointmentCount> Items);
