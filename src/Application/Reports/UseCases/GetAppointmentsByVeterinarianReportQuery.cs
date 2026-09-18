using Application.Common.Abstractions;
using Application.Reports.Models;
using FluentValidation;
using MediatR;

namespace Application.Reports.UseCases;

public sealed record GetAppointmentsByVeterinarianReportQuery(DateOnly From, DateOnly To)
    : IRequest<IReadOnlyCollection<AppointmentVeterinarianReport>>;

public sealed class GetAppointmentsByVeterinarianReportQueryValidator
    : AbstractValidator<GetAppointmentsByVeterinarianReportQuery>
{
    private static readonly DateOnly MinimumDate = new(1900, 1, 1);

    public GetAppointmentsByVeterinarianReportQueryValidator()
    {
        RuleFor(x => x.From)
            .GreaterThanOrEqualTo(MinimumDate)
            .WithMessage("El parámetro from es requerido y debe tener formato yyyy-MM-dd.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(MinimumDate)
            .WithMessage("El parámetro to es requerido y debe tener formato yyyy-MM-dd.");

        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .WithMessage("El parámetro from no puede ser posterior a to.");

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber <= 366)
            .WithMessage("El rango máximo permitido es de 366 días.");
    }
}

public sealed class GetAppointmentsByVeterinarianReportQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAppointmentsByVeterinarianReportQuery, IReadOnlyCollection<AppointmentVeterinarianReport>>
{
    private static readonly HashSet<string> ScheduledStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "AGENDADA", "CONFIRMADA", "EN_PROGRESO"
    };

    public async Task<IReadOnlyCollection<AppointmentVeterinarianReport>> Handle(
        GetAppointmentsByVeterinarianReportQuery request,
        CancellationToken cancellationToken)
    {
        var fromInclusive = request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var appointments = await unitOfWork.AppointmentsRepository.GetForVeterinarianReportAsync(
            fromInclusive,
            toExclusive,
            cancellationToken);

        return appointments
            .GroupBy(appointment => new { appointment.VeterinarianId, appointment.VeterinarianName })
            .Select(group => new AppointmentVeterinarianReport(
                group.Key.VeterinarianId,
                group.Key.VeterinarianName,
                group.Count(),
                group.Count(x => string.Equals(x.StatusName, "ATENDIDA", StringComparison.OrdinalIgnoreCase)),
                group.Count(x => string.Equals(x.StatusName, "CANCELADA", StringComparison.OrdinalIgnoreCase)),
                group.Count(x => ScheduledStatuses.Contains(x.StatusName)),
                group.Count(x => !string.Equals(x.StatusName, "ATENDIDA", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x.StatusName, "CANCELADA", StringComparison.OrdinalIgnoreCase)
                    && !ScheduledStatuses.Contains(x.StatusName))))
            .OrderByDescending(report => report.TotalAppointments)
            .ThenBy(report => report.VeterinarianName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
