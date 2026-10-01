using Application.Reports.Models;

namespace Application.Reports;

/// <summary>
/// Deterministic ranking for top services. Infrastructure EF OrderBy must mirror this.
/// </summary>
public static class ReportServiceRanking
{
    public static IEnumerable<ServiceAppointmentCount> Order(
        IEnumerable<ServiceAppointmentCount> items) =>
        items
            .OrderByDescending(item => item.AppointmentsCount)
            .ThenBy(item => item.ServiceName, StringComparer.Ordinal)
            .ThenBy(item => item.ServiceId);
}
