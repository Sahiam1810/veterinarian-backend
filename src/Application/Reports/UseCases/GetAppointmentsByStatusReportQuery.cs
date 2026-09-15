using MediatR;

namespace Application.Reports.UseCases;

public sealed record AppointmentStatusReportItemResponse(
    Guid StatusId,
    string StatusName,
    int Count,
    double Percentage);

public sealed record GetAppointmentsByStatusReportQuery(
    DateOnly From,
    DateOnly To) : IRequest<IReadOnlyCollection<AppointmentStatusReportItemResponse>>;
