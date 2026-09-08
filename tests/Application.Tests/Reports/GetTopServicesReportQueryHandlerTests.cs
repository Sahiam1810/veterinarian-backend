using Application.Reports;
using Application.Reports.Abstraction;
using Application.Reports.Models;
using Application.Reports.UseCases;
using NSubstitute;
using Xunit;

namespace Application.Tests.Reports;

public sealed class ReportPeriodFilterTests
{
    private static readonly DateTime Start = Utc(2026, 9, 1, 0, 0, 0);
    private static readonly DateTime EndExclusive = Utc(2026, 9, 8, 0, 0, 0);

    [Fact]
    public void Contains_includes_range_start()
    {
        Assert.True(ReportPeriodFilter.Contains(Start, Start, EndExclusive));
    }

    [Fact]
    public void Contains_excludes_range_end_exclusive()
    {
        Assert.False(ReportPeriodFilter.Contains(EndExclusive, Start, EndExclusive));
    }

    [Fact]
    public void Contains_excludes_instants_outside_period()
    {
        Assert.False(ReportPeriodFilter.Contains(Utc(2026, 8, 31, 23, 59, 59), Start, EndExclusive));
        Assert.False(ReportPeriodFilter.Contains(Utc(2026, 9, 8, 0, 0, 1), Start, EndExclusive));
        Assert.True(ReportPeriodFilter.Contains(Utc(2026, 9, 7, 23, 59, 59), Start, EndExclusive));
    }

    private static DateTime Utc(int y, int m, int d, int h, int min, int s) =>
        DateTime.SpecifyKind(new DateTime(y, m, d, h, min, s), DateTimeKind.Utc);
}

public sealed class ReportAppointmentStatusTotalsTests
{
    [Fact]
    public void From_maps_required_statuses()
    {
        var bucket = ReportAppointmentStatusTotals.From(
        [
            (ReportAppointmentStatusTotals.Attended, 4),
            (ReportAppointmentStatusTotals.Canceled, 2),
            (ReportAppointmentStatusTotals.NoShow, 3),
            (ReportAppointmentStatusTotals.Scheduled, 5)
        ]);

        Assert.Equal(4, bucket.AttendedCount);
        Assert.Equal(2, bucket.CanceledCount);
        Assert.Equal(3, bucket.NoShowCount);
        Assert.Equal(5, bucket.ScheduledCount);
    }

    [Fact]
    public void From_does_not_count_CONFIRMADA_or_EN_PROGRESO_as_scheduled()
    {
        var bucket = ReportAppointmentStatusTotals.From(
        [
            (ReportAppointmentStatusTotals.Scheduled, 2),
            (ReportAppointmentStatusTotals.Confirmed, 7),
            (ReportAppointmentStatusTotals.InProgress, 4)
        ]);

        Assert.Equal(2, bucket.ScheduledCount);
    }
}

public sealed class ReportServiceRankingTests
{
    [Fact]
    public void Order_ranks_by_count_desc_then_name_asc_then_service_id()
    {
        var serviceA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var serviceB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        var serviceC = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

        var ordered = ReportServiceRanking.Order(
            [
                new ServiceAppointmentCount(serviceC, "Alpha", 3),
                new ServiceAppointmentCount(serviceB, "Beta", 4),
                new ServiceAppointmentCount(serviceA, "Alpha", 3)
            ])
            .Select(item => item.ServiceId)
            .ToArray();

        Assert.Equal([serviceB, serviceA, serviceC], ordered);
    }
}

public sealed class GetTopServicesReportQueryHandlerTests
{
    private static readonly DateTime Start = Utc(2026, 9, 1);
    private static readonly DateTime End = Utc(2026, 9, 8);

    [Fact]
    public async Task Handle_preserves_repository_ranking_order()
    {
        var serviceA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var serviceB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        var serviceC = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetTopServicesAsync(Start, End, 5, Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(
                10,
                [
                    new ServiceAppointmentCount(serviceB, "Beta", 4),
                    new ServiceAppointmentCount(serviceA, "Alpha", 3),
                    new ServiceAppointmentCount(serviceC, "Alpha", 3)
                ]));

        var result = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, End, Take: 5), CancellationToken.None);

        Assert.Equal([serviceB, serviceA, serviceC], result.Select(item => item.ServiceId).ToArray());
    }

    [Fact]
    public async Task Handle_percentage_uses_full_period_total_not_top_partial_sum()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetTopServicesAsync(Start, End, 2, Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(
                TotalAppointments: 20,
                Items:
                [
                    new ServiceAppointmentCount(Guid.NewGuid(), "Consulta", 5),
                    new ServiceAppointmentCount(Guid.NewGuid(), "Vacuna", 3)
                ]));

        var result = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, End, Take: 2), CancellationToken.None);

        Assert.Equal(25.00m, result[0].Percentage);
        Assert.Equal(15.00m, result[1].Percentage);
        Assert.NotEqual(62.50m, result[0].Percentage);
    }

    [Fact]
    public async Task Handle_returns_empty_list_when_total_is_zero()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetTopServicesAsync(Start, End, 5, Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>()));

        var result = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, End), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_forwards_take_limit_to_repository()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetTopServicesAsync(Start, End, 3, Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(
                10,
                [
                    new ServiceAppointmentCount(Guid.NewGuid(), "A", 4),
                    new ServiceAppointmentCount(Guid.NewGuid(), "B", 3),
                    new ServiceAppointmentCount(Guid.NewGuid(), "C", 2)
                ]));

        var result = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, End, Take: 3), CancellationToken.None);

        Assert.Equal(3, result.Count);
        await repository.Received(1)
            .GetTopServicesAsync(Start, End, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_uses_single_top_services_repository_call()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetTopServicesAsync(Start, End, 5, Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(
                4,
                [new ServiceAppointmentCount(Guid.NewGuid(), "Consulta", 4)]));

        _ = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, End), CancellationToken.None);

        await repository.Received(1)
            .GetTopServicesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive()
            .GetSummaryAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static DateTime Utc(int y, int m, int d) =>
        DateTime.SpecifyKind(new DateTime(y, m, d, 0, 0, 0), DateTimeKind.Utc);
}

public sealed class GetAppointmentsSummaryReportQueryHandlerTests
{
    private static readonly DateTime Start = Utc(2026, 9, 1);
    private static readonly DateTime End = Utc(2026, 9, 8);

    [Fact]
    public async Task Handle_maps_status_buckets_and_attendance_rate()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetSummaryAsync(Start, End, Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(
                TotalAppointments: 10,
                AttendedCount: 4,
                CanceledCount: 2,
                NoShowCount: 3,
                ScheduledCount: 1,
                TopService: null));

        var result = await new GetAppointmentsSummaryReportQueryHandler(repository)
            .Handle(new GetAppointmentsSummaryReportQuery(Start, End), CancellationToken.None);

        Assert.Equal(4, result.AttendedCount);
        Assert.Equal(2, result.CanceledCount);
        Assert.Equal(3, result.NoShowCount);
        Assert.Equal(1, result.ScheduledCount);
        Assert.Equal(40.00m, result.AttendanceRate);
    }

    [Fact]
    public async Task Handle_attendance_rate_is_zero_when_total_is_zero()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetSummaryAsync(Start, End, Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(0, 0, 0, 0, 0, null));

        var result = await new GetAppointmentsSummaryReportQueryHandler(repository)
            .Handle(new GetAppointmentsSummaryReportQuery(Start, End), CancellationToken.None);

        Assert.Equal(0, result.TotalAppointments);
        Assert.Equal(0m, result.AttendanceRate);
        Assert.Null(result.TopServiceId);
        Assert.Null(result.TopServiceName);
        Assert.Equal(0, result.TopServiceCount);
        Assert.Equal(0m, result.TopServicePercentage);
    }

    [Fact]
    public async Task Handle_top_service_matches_take_one_of_top_services()
    {
        var topServiceId = Guid.Parse("dddddddd-0000-0000-0000-000000000004");
        var top = new ServiceAppointmentCount(topServiceId, "Consulta", 7);

        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetSummaryAsync(Start, End, Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(
                TotalAppointments: 10,
                AttendedCount: 5,
                CanceledCount: 1,
                NoShowCount: 1,
                ScheduledCount: 3,
                TopService: top));
        repository.GetTopServicesAsync(Start, End, 1, Arg.Any<CancellationToken>())
            .Returns(new TopServicesReadResult(10, [top]));

        var summary = await new GetAppointmentsSummaryReportQueryHandler(repository)
            .Handle(new GetAppointmentsSummaryReportQuery(Start, End), CancellationToken.None);
        var topOnly = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, End, Take: 1), CancellationToken.None);

        Assert.Single(topOnly);
        Assert.Equal(topOnly[0].ServiceId, summary.TopServiceId);
        Assert.Equal(topOnly[0].ServiceName, summary.TopServiceName);
        Assert.Equal(topOnly[0].AppointmentsCount, summary.TopServiceCount);
        Assert.Equal(topOnly[0].Percentage, summary.TopServicePercentage);
    }

    [Fact]
    public async Task Handle_uses_single_summary_aggregate_call()
    {
        var repository = Substitute.For<IReportsReadRepository>();
        repository.GetSummaryAsync(Start, End, Arg.Any<CancellationToken>())
            .Returns(new AppointmentsSummaryReadResult(
                5,
                2,
                1,
                1,
                1,
                new ServiceAppointmentCount(Guid.NewGuid(), "Consulta", 3)));

        _ = await new GetAppointmentsSummaryReportQueryHandler(repository)
            .Handle(new GetAppointmentsSummaryReportQuery(Start, End), CancellationToken.None);

        await repository.Received(1)
            .GetSummaryAsync(Start, End, Arg.Any<CancellationToken>());
        await repository.DidNotReceive()
            .GetTopServicesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static DateTime Utc(int y, int m, int d) =>
        DateTime.SpecifyKind(new DateTime(y, m, d, 0, 0, 0), DateTimeKind.Utc);
}

public sealed class FakeReportsReadRepositoryPeriodTests
{
    private static readonly DateTime Start = Utc(2026, 9, 1, 0, 0, 0);
    private static readonly DateTime EndExclusive = Utc(2026, 9, 8, 0, 0, 0);

    private static readonly Guid ServiceConsulta = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ServiceVacuna = Guid.Parse("22222222-0000-0000-0000-000000000002");

    [Fact]
    public async Task TopServices_half_open_includes_start_excludes_end_and_outside()
    {
        var repository = new InMemoryReportsReadRepository(
        [
            Row(Start, ServiceConsulta, "Consulta", ReportAppointmentStatusTotals.Attended),
            Row(EndExclusive.AddTicks(-1), ServiceConsulta, "Consulta", ReportAppointmentStatusTotals.Scheduled),
            Row(EndExclusive, ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.Attended),
            Row(Start.AddDays(-1), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.Attended)
        ]);

        var result = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, EndExclusive, Take: 5), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(ServiceConsulta, result[0].ServiceId);
        Assert.Equal(2, result[0].AppointmentsCount);
        Assert.Equal(100.00m, result[0].Percentage);
    }

    [Fact]
    public async Task Summary_scheduledCount_excludes_CONFIRMADA_and_EN_PROGRESO()
    {
        var repository = new InMemoryReportsReadRepository(
        [
            Row(Utc(2026, 9, 2, 10, 0, 0), ServiceConsulta, "Consulta", ReportAppointmentStatusTotals.Scheduled),
            Row(Utc(2026, 9, 3, 10, 0, 0), ServiceConsulta, "Consulta", ReportAppointmentStatusTotals.Confirmed),
            Row(Utc(2026, 9, 4, 10, 0, 0), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.InProgress),
            Row(Utc(2026, 9, 5, 10, 0, 0), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.Attended),
            Row(Utc(2026, 9, 6, 10, 0, 0), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.Canceled),
            Row(Utc(2026, 9, 7, 10, 0, 0), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.NoShow)
        ]);

        var summary = await new GetAppointmentsSummaryReportQueryHandler(repository)
            .Handle(new GetAppointmentsSummaryReportQuery(Start, EndExclusive), CancellationToken.None);

        Assert.Equal(6, summary.TotalAppointments);
        Assert.Equal(1, summary.AttendedCount);
        Assert.Equal(1, summary.CanceledCount);
        Assert.Equal(1, summary.NoShowCount);
        Assert.Equal(1, summary.ScheduledCount);
        Assert.Equal(Math.Round(100m / 6m, 2, MidpointRounding.AwayFromZero), summary.AttendanceRate);
    }

    [Fact]
    public async Task Summary_top_matches_top_services_take_1_with_same_ranking()
    {
        var repository = new InMemoryReportsReadRepository(
        [
            Row(Utc(2026, 9, 2, 10, 0, 0), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.Attended),
            Row(Utc(2026, 9, 3, 10, 0, 0), ServiceVacuna, "Vacuna", ReportAppointmentStatusTotals.Attended),
            Row(Utc(2026, 9, 4, 10, 0, 0), ServiceConsulta, "Consulta", ReportAppointmentStatusTotals.Attended),
            Row(Utc(2026, 9, 5, 10, 0, 0), ServiceConsulta, "Consulta", ReportAppointmentStatusTotals.Attended)
        ]);

        var summary = await new GetAppointmentsSummaryReportQueryHandler(repository)
            .Handle(new GetAppointmentsSummaryReportQuery(Start, EndExclusive), CancellationToken.None);
        var top = await new GetTopServicesReportQueryHandler(repository)
            .Handle(new GetTopServicesReportQuery(Start, EndExclusive, Take: 1), CancellationToken.None);

        Assert.Single(top);
        Assert.Equal(ServiceConsulta, top[0].ServiceId);
        Assert.Equal(summary.TopServiceId, top[0].ServiceId);
        Assert.Equal(summary.TopServiceName, top[0].ServiceName);
        Assert.Equal(summary.TopServiceCount, top[0].AppointmentsCount);
        Assert.Equal(summary.TopServicePercentage, top[0].Percentage);
    }

    private static InMemoryAppointmentRow Row(
        DateTime scheduledStartUtc,
        Guid serviceId,
        string serviceName,
        string statusName) =>
        new(scheduledStartUtc, serviceId, serviceName, statusName);

    private static DateTime Utc(int y, int m, int d, int h, int min, int s) =>
        DateTime.SpecifyKind(new DateTime(y, m, d, h, min, s), DateTimeKind.Utc);
}

internal sealed record InMemoryAppointmentRow(
    DateTime ScheduledStartUtc,
    Guid ServiceId,
    string ServiceName,
    string StatusName);

/// <summary>
/// In-memory stand-in that applies the same period/status/ranking helpers as Infrastructure.
/// </summary>
internal sealed class InMemoryReportsReadRepository(IReadOnlyList<InMemoryAppointmentRow> rows)
    : IReportsReadRepository
{
    public Task<TopServicesReadResult> GetTopServicesAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        var period = rows
            .Where(row => ReportPeriodFilter.Contains(
                row.ScheduledStartUtc,
                rangeStartUtc,
                rangeEndExclusiveUtc))
            .ToList();

        if (period.Count == 0)
        {
            return Task.FromResult(new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>()));
        }

        var items = ReportServiceRanking.Order(
                period
                    .GroupBy(row => (row.ServiceId, row.ServiceName))
                    .Select(group => new ServiceAppointmentCount(
                        group.Key.ServiceId,
                        group.Key.ServiceName,
                        group.Count())))
            .Take(take)
            .ToList();

        return Task.FromResult(new TopServicesReadResult(period.Count, items));
    }

    public async Task<AppointmentsSummaryReadResult> GetSummaryAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        var top = await GetTopServicesAsync(rangeStartUtc, rangeEndExclusiveUtc, take: 1, cancellationToken);
        if (top.TotalAppointments == 0)
        {
            return new AppointmentsSummaryReadResult(0, 0, 0, 0, 0, null);
        }

        var period = rows
            .Where(row => ReportPeriodFilter.Contains(
                row.ScheduledStartUtc,
                rangeStartUtc,
                rangeEndExclusiveUtc));

        var bucket = ReportAppointmentStatusTotals.From(
            period.GroupBy(row => row.StatusName)
                .Select(group => (group.Key, group.Count())));

        return new AppointmentsSummaryReadResult(
            top.TotalAppointments,
            bucket.AttendedCount,
            bucket.CanceledCount,
            bucket.NoShowCount,
            bucket.ScheduledCount,
            top.Items.Count > 0 ? top.Items[0] : null);
    }
}

public sealed class ReportsReportQueryValidatorTests
{
    private static readonly DateTime Start = DateTime.SpecifyKind(new DateTime(2026, 9, 1), DateTimeKind.Utc);
    private static readonly DateTime End = DateTime.SpecifyKind(new DateTime(2026, 9, 8), DateTimeKind.Utc);

    [Fact]
    public void TopServices_rejects_rangeStart_greater_or_equal_rangeEnd()
    {
        var validator = new GetTopServicesReportQueryValidator();

        Assert.False(validator.Validate(new GetTopServicesReportQuery(End, Start, 5)).IsValid);
        Assert.False(validator.Validate(new GetTopServicesReportQuery(Start, Start, 5)).IsValid);
    }

    [Fact]
    public void TopServices_rejects_take_below_1_and_above_20()
    {
        var validator = new GetTopServicesReportQueryValidator();

        Assert.False(validator.Validate(new GetTopServicesReportQuery(Start, End, 0)).IsValid);
        Assert.False(validator.Validate(new GetTopServicesReportQuery(Start, End, 21)).IsValid);
    }

    [Fact]
    public void TopServices_accepts_take_bounds_1_and_20()
    {
        var validator = new GetTopServicesReportQueryValidator();

        Assert.True(validator.Validate(new GetTopServicesReportQuery(Start, End, 1)).IsValid);
        Assert.True(validator.Validate(new GetTopServicesReportQuery(Start, End, 20)).IsValid);
    }

    [Fact]
    public void Summary_rejects_rangeStart_greater_or_equal_rangeEnd()
    {
        var validator = new GetAppointmentsSummaryReportQueryValidator();

        Assert.False(validator.Validate(new GetAppointmentsSummaryReportQuery(End, Start)).IsValid);
        Assert.False(validator.Validate(new GetAppointmentsSummaryReportQuery(Start, Start)).IsValid);
        Assert.True(validator.Validate(new GetAppointmentsSummaryReportQuery(Start, End)).IsValid);
    }
}
