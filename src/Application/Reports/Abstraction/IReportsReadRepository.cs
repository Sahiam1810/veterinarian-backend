using Application.Reports.Models;

namespace Application.Reports.Abstraction;

/// <summary>
/// Read-only aggregates for Reports. Range bounds are already normalized UTC
/// half-open intervals: ScheduledStart &gt;= rangeStartUtc AND ScheduledStart &lt; rangeEndExclusiveUtc.
/// </summary>
public interface IReportsReadRepository
{
    Task<TopServicesReadResult> GetTopServicesAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<AppointmentsSummaryReadResult> GetSummaryAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        CancellationToken cancellationToken = default);
}
