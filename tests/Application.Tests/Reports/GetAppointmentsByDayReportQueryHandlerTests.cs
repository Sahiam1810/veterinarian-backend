using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Reports.UseCases;
using Domain.Appointments.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Reports;

// Tarea B2. No existe una Tarea B1 en el repositorio (verificado en código, ramas e
// historial de git) de la que reutilizar la clasificación de estados o el contrato común;
// estas pruebas cubren la interpretación documentada en
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
        var attended = StatusOf("ATENDIDA");
        var canceled = StatusOf("CANCELADA");
        var agendada = StatusOf("AGENDADA");
        var confirmada = StatusOf("CONFIRMADA");

        // 2026-09-02 12:00 America/Bogota == 2026-09-02 17:00 UTC (Bogota es UTC-5, sin DST).
        var day2Noon = new DateTime(2026, 9, 2, 17, 0, 0, DateTimeKind.Utc);
        var appointments = new[]
        {
            AppointmentWithStatus(day2Noon, attended),
            AppointmentWithStatus(day2Noon, attended),
            AppointmentWithStatus(day2Noon, canceled),
            AppointmentWithStatus(day2Noon, agendada),
            AppointmentWithStatus(day2Noon, confirmada),
            // Medianoche del día siguiente en Bogota (2026-09-04 00:00) == 2026-09-04 05:00 UTC:
            // cae fuera del rango solicitado y no debe contarse en ningún día.
            AppointmentWithStatus(new DateTime(2026, 9, 4, 5, 0, 0, DateTimeKind.Utc), attended),
        };

        appointmentsRepository.GetScheduledBetweenAsync(
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
        appointmentsRepository.GetScheduledBetweenAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());

        await sut.Handle(
            new GetAppointmentsByDayReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)),
            CancellationToken.None);

        await appointmentsRepository.Received(1).GetScheduledBetweenAsync(
            new DateTime(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 4, 5, 0, 0, DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }

    private static StatusAppointment StatusOf(string name) => new(name, null);

    private static Appointment AppointmentWithStatus(DateTime scheduledStartUtc, StatusAppointment status)
    {
        var appointment = new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status.Id,
            Guid.NewGuid(),
            scheduledStartUtc,
            scheduledStartUtc.AddMinutes(30),
            null);
        typeof(Appointment).GetProperty(nameof(Appointment.Status))!.SetValue(appointment, status);
        return appointment;
    }

    private sealed class BogotaSettings : IAppointmentBookingSettings
    {
        public string TimeZoneId => "America/Bogota";
        public TimeSpan MinimumLeadTime => TimeSpan.FromMinutes(60);
        public int MaximumAdvanceDays => 30;
    }
}
