using Api.Reports.Dtos;
using Application.Reports.Models;
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

    public static IReadOnlyCollection<TopServiceReportResponse> ToResponse(
        this IReadOnlyList<TopServiceReportItem> items)
    {
        return items.Select(item => new TopServiceReportResponse(
            item.ServiceId,
            item.ServiceName,
            item.AppointmentsCount,
            item.Percentage)).ToArray();
    }

    public static AppointmentsSummaryReportResponse ToResponse(
        this AppointmentsSummaryReport report,
        DateOnly from,
        DateOnly to)
    {
        ReportTopServiceSummaryResponse? topService = null;
        if (report.TopServiceId is not null && report.TopServiceName is not null)
        {
            topService = new ReportTopServiceSummaryResponse(
                report.TopServiceName,
                report.TopServiceCount,
                report.TopServicePercentage);
        }

        return new AppointmentsSummaryReportResponse(
            from,
            to,
            report.TotalAppointments,
            report.AttendedCount,
            report.CanceledCount,
            report.NoShowCount,
            report.ScheduledCount,
            report.AttendanceRate,
            topService);
    }
}
