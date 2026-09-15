namespace Application.Reports;

/// <summary>
/// Maps status-name aggregates into summary buckets.
/// scheduledCount = AGENDADA + CONFIRMADA + EN_PROGRESO (same grouping as B1/B2).
/// </summary>
public static class ReportAppointmentStatusTotals
{
    public const string Attended = "ATENDIDA";
    public const string Canceled = "CANCELADA";
    public const string NoShow = "NO_ASISTIO";
    public const string Scheduled = "AGENDADA";
    public const string Confirmed = "CONFIRMADA";
    public const string InProgress = "EN_PROGRESO";

    public static AppointmentsStatusCountBucket From(
        IEnumerable<(string StatusName, int Count)> statusCounts)
    {
        var attended = 0;
        var canceled = 0;
        var noShow = 0;
        var scheduled = 0;

        foreach (var (statusName, count) in statusCounts)
        {
            if (string.Equals(statusName, Attended, StringComparison.OrdinalIgnoreCase))
            {
                attended += count;
            }
            else if (string.Equals(statusName, Canceled, StringComparison.OrdinalIgnoreCase))
            {
                canceled += count;
            }
            else if (string.Equals(statusName, NoShow, StringComparison.OrdinalIgnoreCase))
            {
                noShow += count;
            }
            else if (string.Equals(statusName, Scheduled, StringComparison.OrdinalIgnoreCase)
                || string.Equals(statusName, Confirmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(statusName, InProgress, StringComparison.OrdinalIgnoreCase))
            {
                scheduled += count;
            }
        }

        return new AppointmentsStatusCountBucket(attended, canceled, noShow, scheduled);
    }
}

public sealed record AppointmentsStatusCountBucket(
    int AttendedCount,
    int CanceledCount,
    int NoShowCount,
    int ScheduledCount);
