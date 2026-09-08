using FluentValidation;

namespace Application.Reports.UseCases;

// Contrato común de rango de fechas para Reports: from <= to. El formato de From/To
// (yyyy-MM-dd) ya lo valida el model binding de DateOnly antes de llegar aquí
// (falla con 400 automático vía [ApiController], igual que en GetAppointmentBookingSlotsQuery).
public sealed class GetAppointmentsByDayReportQueryValidator : AbstractValidator<GetAppointmentsByDayReportQuery>
{
    public GetAppointmentsByDayReportQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("'to' debe ser mayor o igual que 'from'.");
    }
}
