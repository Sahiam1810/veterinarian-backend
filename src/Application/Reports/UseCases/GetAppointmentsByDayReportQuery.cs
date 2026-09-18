using MediatR;

namespace Application.Reports.UseCases;

public sealed record AppointmentsByDayReportItem(
    DateOnly Date,
    int TotalAppointments,
    int AttendedCount,
    int CanceledCount,
    int ScheduledCount);

// Tarea B2. Intervalo [From, To] inclusivo, agrupado por fecha calendario
// America/Bogota (ver settings.TimeZoneId, consistente con GetAppointmentBookingSlotsQuery).
public sealed record GetAppointmentsByDayReportQuery(
    DateOnly From,
    DateOnly To) : IRequest<IReadOnlyCollection<AppointmentsByDayReportItem>>;
