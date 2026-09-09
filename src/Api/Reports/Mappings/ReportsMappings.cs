using Api.Reports.Dtos;
using Application.Reports.UseCases;

namespace Api.Reports.Mappings;

public static class ReportsMappings
{
    public static IReadOnlyCollection<AppointmentsByDayReportResponse> ToResponse(
        this IReadOnlyCollection<AppointmentsByDayReportItem> items)
    {
        return items.Select(item => new AppointmentsByDayReportResponse(
            item.Date,
            item.TotalAppointments,
            item.AttendedCount,
            item.CanceledCount,
            item.ScheduledCount)).ToArray();
    }
}
