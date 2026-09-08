using FluentValidation;

namespace Application.Reports.UseCases;

public sealed class GetAppointmentsByStatusReportQueryValidator
    : AbstractValidator<GetAppointmentsByStatusReportQuery>
{
    public GetAppointmentsByStatusReportQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty()
            .WithMessage("La fecha de inicio 'from' es obligatoria.");

        RuleFor(x => x.To)
            .NotEmpty()
            .WithMessage("La fecha de fin 'to' es obligatoria.");

        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .WithMessage("La fecha inicial 'from' no puede ser posterior a la fecha final 'to'.")
            .When(x => x.From != default && x.To != default);
    }
}
