namespace Application.Reports;

/// <summary>
/// Converts inclusive local DateOnly bounds to a UTC half-open interval
/// using the same America/Bogota pattern as GetAppointmentsByDayReportQueryHandler.
/// </summary>
public static class ReportLocalUtcRange
{
    public const int MaxInclusiveDays = 366;

    public static (DateTime RangeStartUtc, DateTime RangeEndExclusiveUtc) ToHalfOpen(
        DateOnly from,
        DateOnly to,
        string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var rangeStartLocal = from.ToDateTime(TimeOnly.MinValue);
        var rangeEndExclusiveLocal = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return (ToUtc(rangeStartLocal, timeZone), ToUtc(rangeEndExclusiveLocal, timeZone));
    }

    public static int InclusiveDayCount(DateOnly from, DateOnly to) =>
        (to.DayNumber - from.DayNumber) + 1;

    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
}
