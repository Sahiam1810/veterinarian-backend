using Application.Appointments.Abstraction;
using Application.Reports.Models;
using FluentValidation;
using MediatR;

namespace Application.Reports.UseCases;

public sealed record GetAppointmentsSummaryReportByLocalDateQuery(
    DateOnly From,
    DateOnly To) : IRequest<AppointmentsSummaryReport>;

public sealed class GetAppointmentsSummaryReportByLocalDateQueryValidator
    : AbstractValidator<GetAppointmentsSummaryReportByLocalDateQuery>
{
    public GetAppointmentsSummaryReportByLocalDateQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => query.From <= query.To)
            .WithMessage("El parámetro from no puede ser posterior a to.");

        RuleFor(query => query)
            .Must(query => ReportLocalUtcRange.InclusiveDayCount(query.From, query.To)
                <= ReportLocalUtcRange.MaxInclusiveDays)
            .When(query => query.From <= query.To)
            .WithMessage("El rango máximo permitido es de 366 días.");
    }
}

public sealed class GetAppointmentsSummaryReportByLocalDateQueryHandler(
    IAppointmentBookingSettings settings,
    ISender sender)
    : IRequestHandler<GetAppointmentsSummaryReportByLocalDateQuery, AppointmentsSummaryReport>
{
    public Task<AppointmentsSummaryReport> Handle(
        GetAppointmentsSummaryReportByLocalDateQuery request,
        CancellationToken cancellationToken)
    {
        var (rangeStartUtc, rangeEndExclusiveUtc) = ReportLocalUtcRange.ToHalfOpen(
            request.From,
            request.To,
            settings.TimeZoneId);

        return sender.Send(
            new GetAppointmentsSummaryReportQuery(rangeStartUtc, rangeEndExclusiveUtc),
            cancellationToken);
    }
}
