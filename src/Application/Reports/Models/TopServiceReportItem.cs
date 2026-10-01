namespace Application.Reports.Models;

public sealed record TopServiceReportItem(
    Guid ServiceId,
    string ServiceName,
    int AppointmentsCount,
    decimal Percentage);
