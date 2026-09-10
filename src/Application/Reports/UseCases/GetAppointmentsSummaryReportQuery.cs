using Application.Reports.Models;
using MediatR;

namespace Application.Reports.UseCases;

public sealed record GetAppointmentsSummaryReportQuery(
    DateTime RangeStartUtc,
    DateTime RangeEndExclusiveUtc) : IRequest<AppointmentsSummaryReport>;
