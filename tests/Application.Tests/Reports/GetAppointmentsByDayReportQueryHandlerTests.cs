using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Reports.Models;
using Application.Reports.UseCases;
using NSubstitute;
using Xunit;

namespace Application.Tests.Reports;

// Tarea B2. La clasificación de estados es propia de este reporte (no hay un contrato
// común con B1 para eso); estas pruebas cubren la interpretación documentada en
// GetAppointmentsByDayReportQueryHandler para AGENDADA/CONFIRMADA/EN_PROGRESO -> Scheduled,
// ATENDIDA -> Attended y CANCELADA/NO_ASISTIO -> Canceled.
public sealed class GetAppointmentsByDayReportQueryHandlerTests
{
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BogotaSettings settings = new();
    private readonly GetAppointmentsByDayReportQueryHandler sut;

    public GetAppointmentsByDayReportQueryHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        sut = new GetAppointmentsByDayReportQueryHandler(unitOfWork, settings);
    }

    [Fact]
    public async Task Handle_returns_all_days_in_range_with_only_day_two_populated()
    {
        // 2026-09-02 12:00 America/Bogota == 2026-09-02 17:00 UTC (Bogota es UTC-5, sin DST).
        var day2Noon = new DateTime(2026, 9, 2, 17, 0, 0, DateTimeKind.Utc);
        var appointments = new[]
        {
            new AppointmentDayReportEntry(day2Noon, "ATENDIDA"),
            new AppointmentDayReportEntry(day2Noon, "ATENDIDA"),
            new AppointmentDayReportEntry(day2Noon, "CANCELADA"),
            new AppointmentDayReportEntry(day2Noon, "AGENDADA"),
            new AppointmentDayReportEntry(day2Noon, "CONFIRMADA"),
            // Medianoche del día siguiente en Bogota (2026-09-04 00:00) == 2026-09-04 05:00 UTC:
            // cae fuera del rango solicitado y no debe contarse en ningún día.
            new AppointmentDayReportEntry(new DateTime(2026, 9, 4, 5, 0, 0, DateTimeKind.Utc), "ATENDIDA"),
        };

        appointmentsRepository.GetForDayReportAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(appointments);

        var result = await sut.Handle(
            new GetAppointmentsByDayReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)),
            CancellationToken.None);

        var items = result.ToArray();
        Assert.Equal(3, items.Length);
        Assert.Equal(
            new[] { new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 3) },
            items.Select(x => x.Date));

        Assert.Equal(0, items[0].TotalAppointments);
        Assert.Equal(0, items[0].AttendedCount);
        Assert.Equal(0, items[0].CanceledCount);
        Assert.Equal(0, items[0].ScheduledCount);

        Assert.Equal(5, items[1].TotalAppointments);
        Assert.Equal(2, items[1].AttendedCount);
        Assert.Equal(1, items[1].CanceledCount);
        Assert.Equal(2, items[1].ScheduledCount);

        Assert.Equal(0, items[2].TotalAppointments);
        Assert.Equal(0, items[2].AttendedCount);
        Assert.Equal(0, items[2].CanceledCount);
        Assert.Equal(0, items[2].ScheduledCount);
    }

    [Fact]
    public async Task Handle_queries_repository_using_america_bogota_day_boundaries_in_utc()
    {
        appointmentsRepository.GetForDayReportAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppointmentDayReportEntry>());

        await sut.Handle(
            new GetAppointmentsByDayReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)),
            CancellationToken.None);

        await appointmentsRepository.Received(1).GetForDayReportAsync(
            new DateTime(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 4, 5, 0, 0, DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }

    private sealed class BogotaSettings : IAppointmentBookingSettings
    {
        public string TimeZoneId => "America/Bogota";
        public TimeSpan MinimumLeadTime => TimeSpan.FromMinutes(60);
        public int MaximumAdvanceDays => 30;
    }
}
