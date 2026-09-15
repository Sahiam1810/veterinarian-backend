using Application.Appointments.Abstraction;
using Application.Reports.Models;
using FluentValidation;
using MediatR;

namespace Application.Reports.UseCases;

public sealed record GetTopServicesReportByLocalDateQuery(
    DateOnly From,
    DateOnly To,
    int Take = GetTopServicesReportQueryValidator.DefaultTake)
    : IRequest<IReadOnlyList<TopServiceReportItem>>;

public sealed class GetTopServicesReportByLocalDateQueryValidator
    : AbstractValidator<GetTopServicesReportByLocalDateQuery>
{
    public GetTopServicesReportByLocalDateQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => query.From <= query.To)
            .WithMessage("El parámetro from no puede ser posterior a to.");

        RuleFor(query => query)
            .Must(query => ReportLocalUtcRange.InclusiveDayCount(query.From, query.To)
                <= ReportLocalUtcRange.MaxInclusiveDays)
            .When(query => query.From <= query.To)
            .WithMessage("El rango máximo permitido es de 366 días.");

        RuleFor(query => query.Take)
            .InclusiveBetween(1, GetTopServicesReportQueryValidator.MaxTake)
            .WithMessage($"Take must be between 1 and {GetTopServicesReportQueryValidator.MaxTake}.");
    }
}

public sealed class GetTopServicesReportByLocalDateQueryHandler(
    IAppointmentBookingSettings settings,
    ISender sender)
    : IRequestHandler<GetTopServicesReportByLocalDateQuery, IReadOnlyList<TopServiceReportItem>>
{
    public Task<IReadOnlyList<TopServiceReportItem>> Handle(
        GetTopServicesReportByLocalDateQuery request,
        CancellationToken cancellationToken)
    {
        var (rangeStartUtc, rangeEndExclusiveUtc) = ReportLocalUtcRange.ToHalfOpen(
            request.From,
            request.To,
            settings.TimeZoneId);

        return sender.Send(
            new GetTopServicesReportQuery(rangeStartUtc, rangeEndExclusiveUtc, request.Take),
            cancellationToken);
    }
}
