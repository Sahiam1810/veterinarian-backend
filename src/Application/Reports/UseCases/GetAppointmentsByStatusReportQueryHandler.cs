using Application.Common.Abstractions;
using MediatR;

namespace Application.Reports.UseCases;

public sealed class GetAppointmentsByStatusReportQueryHandler
    : IRequestHandler<GetAppointmentsByStatusReportQuery, IReadOnlyCollection<AppointmentStatusReportItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAppointmentsByStatusReportQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<AppointmentStatusReportItemResponse>> Handle(
        GetAppointmentsByStatusReportQuery request,
        CancellationToken cancellationToken)
    {
        var statuses = await _unitOfWork.StatusAppointmentsRepository.GetAllAsync(cancellationToken);

        var fromUtc = request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = request.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var appointments = await _unitOfWork.AppointmentsRepository.GetScheduledBetweenAsync(
            fromUtc,
            toUtc,
            cancellationToken);

        var totalPeriod = appointments.Count;

        var countsByStatusId = appointments
            .GroupBy(a => a.StatusId)
            .ToDictionary(g => g.Key, g => g.Count());

        var reportItems = statuses
            .Select(status =>
            {
                var count = countsByStatusId.TryGetValue(status.Id, out var c) ? c : 0;
                var percentage = totalPeriod > 0
                    ? Math.Round((double)count / totalPeriod * 100.0, 1, MidpointRounding.AwayFromZero)
                    : 0.0;

                return new AppointmentStatusReportItemResponse(
                    status.Id,
                    status.Name,
                    count,
                    percentage);
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.StatusName)
            .ToList();

        return reportItems;
    }
}
