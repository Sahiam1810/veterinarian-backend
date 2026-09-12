namespace Application.Reports;

/// <summary>
/// Half-open UTC period filter for Reports aggregates.
/// Matches Infrastructure: ScheduledStart &gt;= start AND ScheduledStart &lt; endExclusive.
/// </summary>
public static class ReportPeriodFilter
{
    public static bool Contains(
        DateTime scheduledStartUtc,
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc) =>
        scheduledStartUtc >= rangeStartUtc
        && scheduledStartUtc < rangeEndExclusiveUtc;
}
