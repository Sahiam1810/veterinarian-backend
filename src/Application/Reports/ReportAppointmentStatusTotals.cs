namespace Application.Reports;

/// <summary>
/// Maps status-name aggregates into summary buckets.
/// scheduledCount includes only AGENDADA; CONFIRMADA and EN_PROGRESO are excluded
/// until the leader decides otherwise.
/// </summary>
public static class ReportAppointmentStatusTotals
{
    public const string Attended = "ATENDIDA";
    public const string Canceled = "CANCELADA";
    public const string NoShow = "NO_ASISTIO";
    public const string Scheduled = "AGENDADA";

    // Explicitly excluded from scheduledCount in this sub-slice:
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
            else if (string.Equals(statusName, Scheduled, StringComparison.OrdinalIgnoreCase))
            {
                scheduled += count;
            }
            // CONFIRMADA / EN_PROGRESO (and any other status) intentionally ignored for scheduledCount.
        }

        return new AppointmentsStatusCountBucket(attended, canceled, noShow, scheduled);
    }
}

public sealed record AppointmentsStatusCountBucket(
    int AttendedCount,
    int CanceledCount,
    int NoShowCount,
    int ScheduledCount);
