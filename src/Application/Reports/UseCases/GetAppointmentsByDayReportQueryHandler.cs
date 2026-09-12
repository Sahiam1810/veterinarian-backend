using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using MediatR;

namespace Application.Reports.UseCases;

public sealed class GetAppointmentsByDayReportQueryHandler(
    IUnitOfWork unitOfWork,
    IAppointmentBookingSettings settings)
    : IRequestHandler<GetAppointmentsByDayReportQuery, IReadOnlyCollection<AppointmentsByDayReportItem>>
{
    // No existe una Tarea B1 en el repositorio (verificado en código, ramas e historial de
    // git) de la que heredar la clasificación de estados. Esta agrupación es una decisión
    // tomada para B2, basada en el catálogo canónico de STATUS_APPOINTMENTS
    // (database/seeds/status_appointments_seed.sql): ATENDIDA -> Attended;
    // CANCELADA/NO_ASISTIO -> Canceled (la cita no se realizó); AGENDADA/CONFIRMADA/
    // EN_PROGRESO -> Scheduled (aún pendiente/en curso). Cualquier estado futuro no
    // catalogado cae por defecto en Scheduled para mantener totalAppointments == la suma
    // de los tres contadores.
    private static readonly IReadOnlyDictionary<string, AppointmentReportBucket> StatusBuckets =
        new Dictionary<string, AppointmentReportBucket>(StringComparer.OrdinalIgnoreCase)
        {
            ["ATENDIDA"] = AppointmentReportBucket.Attended,
            ["CANCELADA"] = AppointmentReportBucket.Canceled,
            ["NO_ASISTIO"] = AppointmentReportBucket.Canceled,
            ["AGENDADA"] = AppointmentReportBucket.Scheduled,
            ["CONFIRMADA"] = AppointmentReportBucket.Scheduled,
            ["EN_PROGRESO"] = AppointmentReportBucket.Scheduled,
        };

    public async Task<IReadOnlyCollection<AppointmentsByDayReportItem>> Handle(
        GetAppointmentsByDayReportQuery request,
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var fromUtc = ToUtc(request.From.ToDateTime(TimeOnly.MinValue), timeZone);
        var toExclusiveUtc = ToUtc(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);

        var appointments = await unitOfWork.AppointmentsRepository.GetForDayReportAsync(
            fromUtc,
            toExclusiveUtc,
            cancellationToken);

        var days = new SortedDictionary<DateOnly, DayCounters>();
        for (var date = request.From; date <= request.To; date = date.AddDays(1))
        {
            days[date] = new DayCounters();
        }

        foreach (var appointment in appointments)
        {
            var localDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(appointment.ScheduledStart, DateTimeKind.Utc),
                    timeZone));

            // GetForDayReportAsync usa límite superior exclusivo, así que todo resultado
            // cae dentro de [From, To]; TryGetValue es solo una salvaguarda defensiva.
            if (!days.TryGetValue(localDate, out var counters))
            {
                continue;
            }

            counters.Total++;
            var bucket = StatusBuckets.TryGetValue(appointment.StatusName, out var mapped)
                ? mapped
                : AppointmentReportBucket.Scheduled;

            switch (bucket)
            {
                case AppointmentReportBucket.Attended:
                    counters.Attended++;
                    break;
                case AppointmentReportBucket.Canceled:
                    counters.Canceled++;
                    break;
                default:
                    counters.Scheduled++;
                    break;
            }
        }

        return days
            .Select(entry => new AppointmentsByDayReportItem(
                entry.Key,
                entry.Value.Total,
                entry.Value.Attended,
                entry.Value.Canceled,
                entry.Value.Scheduled))
            .ToArray();
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);

    private enum AppointmentReportBucket
    {
        Scheduled,
        Attended,
        Canceled
    }

    private sealed class DayCounters
    {
        public int Total;
        public int Attended;
        public int Canceled;
        public int Scheduled;
    }
}
