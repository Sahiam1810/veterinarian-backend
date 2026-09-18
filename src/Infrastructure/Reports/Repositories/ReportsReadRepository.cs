using Application.Reports;
using Application.Reports.Abstraction;
using Application.Reports.Models;
using Domain.Appointments.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reports.Repositories;

public sealed class ReportsReadRepository(VeterinaryDbContext context) : IReportsReadRepository
{
    public async Task<TopServicesReadResult> GetTopServicesAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        var period = PeriodQuery(rangeStartUtc, rangeEndExclusiveUtc);

        var totalAppointments = await period.CountAsync(cancellationToken);
        if (totalAppointments == 0)
        {
            return new TopServicesReadResult(0, Array.Empty<ServiceAppointmentCount>());
        }

        // Ranking must stay aligned with ReportServiceRanking:
        // appointmentsCount DESC, serviceName ASC, serviceId ASC.
        var rows = await period
            .GroupBy(appointment => new
            {
                appointment.ServiceId,
                ServiceName = appointment.Service!.Name
            })
            .Select(group => new
            {
                group.Key.ServiceId,
                group.Key.ServiceName,
                AppointmentsCount = group.Count()
            })
            .OrderByDescending(item => item.AppointmentsCount)
            .ThenBy(item => item.ServiceName)
            .ThenBy(item => item.ServiceId)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new ServiceAppointmentCount(
                row.ServiceId,
                row.ServiceName,
                row.AppointmentsCount))
            .ToList();

        return new TopServicesReadResult(totalAppointments, items);
    }

    public async Task<AppointmentsSummaryReadResult> GetSummaryAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        // Reuse top-services take=1 so ranking/desempate stay identical.
        var topResult = await GetTopServicesAsync(
            rangeStartUtc,
            rangeEndExclusiveUtc,
            take: 1,
            cancellationToken);

        if (topResult.TotalAppointments == 0)
        {
            return new AppointmentsSummaryReadResult(
                TotalAppointments: 0,
                AttendedCount: 0,
                CanceledCount: 0,
                NoShowCount: 0,
                ScheduledCount: 0,
                TopService: null);
        }

        var statusCounts = await PeriodQuery(rangeStartUtc, rangeEndExclusiveUtc)
            .GroupBy(appointment => appointment.Status!.Name)
            .Select(group => new { StatusName = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var bucket = ReportAppointmentStatusTotals.From(
            statusCounts.Select(row => (row.StatusName, row.Count)));

        var topService = topResult.Items.Count > 0 ? topResult.Items[0] : null;

        return new AppointmentsSummaryReadResult(
            topResult.TotalAppointments,
            bucket.AttendedCount,
            bucket.CanceledCount,
            bucket.NoShowCount,
            bucket.ScheduledCount,
            topService);
    }

    // Same half-open semantics as ReportPeriodFilter.Contains.
    private IQueryable<Appointment> PeriodQuery(
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc) =>
        context.Set<Appointment>()
            .AsNoTracking()
            .Where(appointment =>
                appointment.ScheduledStart >= rangeStartUtc
                && appointment.ScheduledStart < rangeEndExclusiveUtc);
}
