using FluentValidation;

namespace Application.Reports.UseCases;

public sealed class GetTopServicesReportQueryValidator
    : AbstractValidator<GetTopServicesReportQuery>
{
    public const int DefaultTake = 5;
    public const int MaxTake = 20;

    public GetTopServicesReportQueryValidator()
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

        RuleFor(query => query.Take)
            .InclusiveBetween(1, MaxTake)
            .WithMessage($"Take must be between 1 and {MaxTake}.");
    }
}
