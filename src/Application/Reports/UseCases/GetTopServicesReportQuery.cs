using Application.Reports.Models;
using MediatR;

namespace Application.Reports.UseCases;

public sealed record GetTopServicesReportQuery(
    DateTime RangeStartUtc,
    DateTime RangeEndExclusiveUtc,
    int Take = 5) : IRequest<IReadOnlyList<TopServiceReportItem>>;
