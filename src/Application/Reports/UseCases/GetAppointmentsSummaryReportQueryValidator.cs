using FluentValidation;

namespace Application.Reports.UseCases;

public sealed class GetAppointmentsSummaryReportQueryValidator
    : AbstractValidator<GetAppointmentsSummaryReportQuery>
{
    public GetAppointmentsSummaryReportQueryValidator()
    {
        RuleFor(query => query.RangeStartUtc)
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("RangeStartUtc must be UTC.");

        RuleFor(query => query.RangeEndExclusiveUtc)
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("RangeEndExclusiveUtc must be UTC.");

        RuleFor(query => query)
            .Must(query => query.RangeStartUtc < query.RangeEndExclusiveUtc)
            .WithMessage("RangeStartUtc must be earlier than RangeEndExclusiveUtc.");
    }
}
