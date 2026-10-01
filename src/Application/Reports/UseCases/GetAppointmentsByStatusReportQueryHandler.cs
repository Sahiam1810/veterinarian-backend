using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using MediatR;

namespace Application.Reports.UseCases;

public sealed class GetAppointmentsByStatusReportQueryHandler
    : IRequestHandler<GetAppointmentsByStatusReportQuery, IReadOnlyCollection<AppointmentStatusReportItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentBookingSettings _bookingSettings;

    public GetAppointmentsByStatusReportQueryHandler(
        IUnitOfWork unitOfWork,
        IAppointmentBookingSettings bookingSettings)
    {
        _unitOfWork = unitOfWork;
        _bookingSettings = bookingSettings;
    }

    public async Task<IReadOnlyCollection<AppointmentStatusReportItemResponse>> Handle(
        GetAppointmentsByStatusReportQuery request,
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_bookingSettings.TimeZoneId);

        var fromLocal = request.From.ToDateTime(TimeOnly.MinValue);
        var toLocalExclusive = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var fromInclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(fromLocal, timeZone);
        var toExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(toLocalExclusive, timeZone);

        var statuses = await _unitOfWork.StatusAppointmentsRepository.GetAllAsync(cancellationToken);

        var countsByStatusId = await _unitOfWork.AppointmentsRepository.GetStatusCountsBetweenAsync(
            fromInclusiveUtc,
            toExclusiveUtc,
            cancellationToken);

        var totalPeriod = countsByStatusId.Values.Sum();

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
