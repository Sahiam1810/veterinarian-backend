using Application.Reports.Abstraction;
using Application.Reports.Models;
using MediatR;

namespace Application.Reports.UseCases;

public sealed class GetTopServicesReportQueryHandler(
    IReportsReadRepository reportsReadRepository)
    : IRequestHandler<GetTopServicesReportQuery, IReadOnlyList<TopServiceReportItem>>
{
    public async Task<IReadOnlyList<TopServiceReportItem>> Handle(
        GetTopServicesReportQuery request,
        CancellationToken cancellationToken)
    {
        var result = await reportsReadRepository.GetTopServicesAsync(
            request.RangeStartUtc,
            request.RangeEndExclusiveUtc,
            request.Take,
            cancellationToken);

        if (result.TotalAppointments == 0 || result.Items.Count == 0)
        {
            return Array.Empty<TopServiceReportItem>();
        }

        return result.Items
            .Select(item => new TopServiceReportItem(
                item.ServiceId,
                item.ServiceName,
                item.AppointmentsCount,
                ReportPercentage.OfTotal(item.AppointmentsCount, result.TotalAppointments)))
            .ToList();
    }
}
