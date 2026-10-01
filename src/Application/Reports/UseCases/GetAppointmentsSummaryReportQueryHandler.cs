using Application.Reports.Abstraction;
using Application.Reports.Models;
using MediatR;

namespace Application.Reports.UseCases;

public sealed class GetAppointmentsSummaryReportQueryHandler(
    IReportsReadRepository reportsReadRepository)
    : IRequestHandler<GetAppointmentsSummaryReportQuery, AppointmentsSummaryReport>
{
    public async Task<AppointmentsSummaryReport> Handle(
        GetAppointmentsSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var result = await reportsReadRepository.GetSummaryAsync(
            request.RangeStartUtc,
            request.RangeEndExclusiveUtc,
            cancellationToken);

        var attendanceRate = ReportPercentage.OfTotal(
            result.AttendedCount,
            result.TotalAppointments);

        if (result.TopService is null)
        {
            return new AppointmentsSummaryReport(
                result.TotalAppointments,
                result.AttendedCount,
                result.CanceledCount,
                result.NoShowCount,
                result.ScheduledCount,
                attendanceRate,
                TopServiceId: null,
                TopServiceName: null,
                TopServiceCount: 0,
                TopServicePercentage: 0m);
        }

        return new AppointmentsSummaryReport(
            result.TotalAppointments,
            result.AttendedCount,
            result.CanceledCount,
            result.NoShowCount,
            result.ScheduledCount,
            attendanceRate,
            result.TopService.ServiceId,
            result.TopService.ServiceName,
            result.TopService.AppointmentsCount,
            ReportPercentage.OfTotal(
                result.TopService.AppointmentsCount,
                result.TotalAppointments));
    }
}
