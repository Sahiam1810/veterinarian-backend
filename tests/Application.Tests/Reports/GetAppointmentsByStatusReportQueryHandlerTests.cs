using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Reports.UseCases;
using Application.StatusAppointments.Abstraction;
using Domain.Appointments.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Reports;

public sealed class GetAppointmentsByStatusReportQueryHandlerTests
{
    private readonly IStatusAppointmentRepository _statusAppointmentRepository = Substitute.For<IStatusAppointmentRepository>();
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetAppointmentsByStatusReportQueryHandler _sut;

    public GetAppointmentsByStatusReportQueryHandlerTests()
    {
        _unitOfWork.StatusAppointmentsRepository.Returns(_statusAppointmentRepository);
        _unitOfWork.AppointmentsRepository.Returns(_appointmentRepository);
        _sut = new GetAppointmentsByStatusReportQueryHandler(_unitOfWork);
    }

    [Fact]
    public async Task Handle_CatalogWith3Statuses_AppointmentsIn2_ReturnsThirdWithZeroCountAndCorrectPercentages()
    {
        // Arrange
        var statusAgendada = new StatusAppointment("AGENDADA", "Cita agendada");
        var statusAtendida = new StatusAppointment("ATENDIDA", "Cita atendida");
        var statusCancelada = new StatusAppointment("CANCELADA", "Cita cancelada");

        var catalog = new List<StatusAppointment> { statusAgendada, statusAtendida, statusCancelada };
        _statusAppointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(catalog);

        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);

        var appt1 = CreateAppointment(statusAgendada.Id, new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc));
        var appt2 = CreateAppointment(statusAgendada.Id, new DateTime(2026, 9, 10, 11, 0, 0, DateTimeKind.Utc));
        var appt3 = CreateAppointment(statusAtendida.Id, new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc));

        var appointments = new List<Appointment> { appt1, appt2, appt3 };

        _appointmentRepository.GetScheduledBetweenAsync(
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(appointments);

        var query = new GetAppointmentsByStatusReportQuery(from, to);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);

        var itemAgendada = Assert.Single(result, x => x.StatusId == statusAgendada.Id);
        Assert.Equal("AGENDADA", itemAgendada.StatusName);
        Assert.Equal(2, itemAgendada.Count);
        Assert.Equal(66.7, itemAgendada.Percentage);

        var itemAtendida = Assert.Single(result, x => x.StatusId == statusAtendida.Id);
        Assert.Equal("ATENDIDA", itemAtendida.StatusName);
        Assert.Equal(1, itemAtendida.Count);
        Assert.Equal(33.3, itemAtendida.Percentage);

        var itemCancelada = Assert.Single(result, x => x.StatusId == statusCancelada.Id);
        Assert.Equal("CANCELADA", itemCancelada.StatusName);
        Assert.Equal(0, itemCancelada.Count);
        Assert.Equal(0.0, itemCancelada.Percentage);
    }

    [Fact]
    public async Task Handle_NoAppointmentsInPeriod_ReturnsAllStatusesWithZeroCountAndZeroPercentage()
    {
        // Arrange
        var statusAgendada = new StatusAppointment("AGENDADA", "Cita agendada");
        var statusAtendida = new StatusAppointment("ATENDIDA", "Cita atendida");

        _statusAppointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StatusAppointment> { statusAgendada, statusAtendida });

        _appointmentRepository.GetScheduledBetweenAsync(
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        var query = new GetAppointmentsByStatusReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item =>
        {
            Assert.Equal(0, item.Count);
            Assert.Equal(0.0, item.Percentage);
        });
    }

    [Fact]
    public async Task Handle_OrdersByCountDescending()
    {
        // Arrange
        var status1 = new StatusAppointment("CANCELADA", "Cita cancelada");
        var status2 = new StatusAppointment("ATENDIDA", "Cita atendida");

        _statusAppointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StatusAppointment> { status1, status2 });

        var appt1 = CreateAppointment(status2.Id, new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc));
        var appt2 = CreateAppointment(status2.Id, new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc));

        _appointmentRepository.GetScheduledBetweenAsync(
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appt1, appt2 });

        var query = new GetAppointmentsByStatusReportQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var first = result.First();
        Assert.Equal("ATENDIDA", first.StatusName);
        Assert.Equal(2, first.Count);

        var last = result.Last();
        Assert.Equal("CANCELADA", last.StatusName);
        Assert.Equal(0, last.Count);
    }

    private static Appointment CreateAppointment(Guid statusId, DateTime scheduledStart)
    {
        return new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            statusId,
            Guid.NewGuid(),
            scheduledStart,
            scheduledStart.AddMinutes(30),
            "Notas de prueba");
    }
}
