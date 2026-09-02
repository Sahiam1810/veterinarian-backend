using Application;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Domain.AppointmentStatusHistories.Entities;
using Domain.Appointments.Entities;
using Domain.StatusAppointments.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class UpdateAppointmentStatusCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AgendadaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AtendidaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid CanceladaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly Guid NoAsistioId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IStatusAppointmentRepository statusAppointmentsRepository = Substitute.For<IStatusAppointmentRepository>();
    private readonly IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository = Substitute.For<IAppointmentStatusHistoryRepository>();
    private readonly UpdateAppointmentStatusCommandHandler sut;

    public UpdateAppointmentStatusCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.StatusAppointmentsRepository.Returns(statusAppointmentsRepository);
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(appointmentStatusHistoriesRepository);
        sut = new UpdateAppointmentStatusCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task STA_T01_Handle_transitions_AGENDADA_to_ATENDIDA()
    {
        var fixture = CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "ATENDIDA");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null);

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(AtendidaId, fixture.Appointment.StatusId);
        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await appointmentsRepository.Received(1).UpdateAsync(fixture.Appointment, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T02_Handle_transitions_AGENDADA_to_CANCELADA_with_comment()
    {
        var fixture = CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "CANCELADA");
        const string comment = "Cliente canceló";
        var command = new UpdateAppointmentStatusCommand(AppointmentId, CanceladaId, comment);

        await sut.Handle(command, CancellationToken.None);

        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Is<AppointmentStatusHistory>(h =>
                h.AppointmentId == AppointmentId
                && h.StatusId == CanceladaId
                && h.ClientPetId == ClientPetId
                && h.Comment == comment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T03_Handle_transitions_AGENDADA_to_NO_ASISTIO_with_comment()
    {
        var fixture = CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "NO_ASISTIO");
        const string comment = "No se presentó";
        var command = new UpdateAppointmentStatusCommand(AppointmentId, NoAsistioId, comment);

        await sut.Handle(command, CancellationToken.None);

        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Is<AppointmentStatusHistory>(h =>
                h.AppointmentId == AppointmentId
                && h.StatusId == NoAsistioId
                && h.ClientPetId == ClientPetId
                && h.Comment == comment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T04_Handle_rejects_AGENDADA_to_CANCELADA_without_comment()
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "CANCELADA");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, CanceladaId, null);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
        await appointmentStatusHistoriesRepository.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T05_Handle_rejects_AGENDADA_to_NO_ASISTIO_with_whitespace_comment()
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "NO_ASISTIO");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, NoAsistioId, "   ");

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
        await appointmentStatusHistoriesRepository.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("ATENDIDA", "CANCELADA")]
    [InlineData("CANCELADA", "ATENDIDA")]
    [InlineData("NO_ASISTIO", "ATENDIDA")]
    public async Task STA_T06_T07_T08_Handle_rejects_disallowed_transitions(
        string currentStatusName,
        string targetStatusName)
    {
        CreateFixture(currentStatusName, targetStatusName);
        var targetId = ResolveStatusId(targetStatusName);
        var command = new UpdateAppointmentStatusCommand(AppointmentId, targetId, "motivo");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
        await appointmentStatusHistoriesRepository.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T09_Handle_throws_NotFoundException_when_appointment_is_missing()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task STA_T10_Handle_throws_ConflictException_when_current_status_is_missing()
    {
        var appointment = CreateAppointment(AgendadaId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(AgendadaId, Arg.Any<CancellationToken>())
            .Returns((StatusAppointment?)null);

        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task STA_T11_Handle_throws_NotFoundException_when_target_status_is_missing()
    {
        var appointment = CreateAppointment(AgendadaId);
        var currentStatus = CreateStatus("AGENDADA", AgendadaId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(AgendadaId, Arg.Any<CancellationToken>())
            .Returns(currentStatus);
        statusAppointmentsRepository.GetByIdAsync(AtendidaId, Arg.Any<CancellationToken>())
            .Returns((StatusAppointment?)null);

        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task STA_T12_Handle_creates_history_with_expected_fields()
    {
        CreateFixture(currentStatusName: "agendada", targetStatusName: "atendida");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, "ok");

        await sut.Handle(command, CancellationToken.None);

        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Is<AppointmentStatusHistory>(h =>
                h.AppointmentId == AppointmentId
                && h.StatusId == AtendidaId
                && h.ClientPetId == ClientPetId
                && h.Comment == "ok"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T13_Handle_updates_appointment_status_id()
    {
        var fixture = CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "ATENDIDA");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null);

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(AtendidaId, fixture.Appointment.StatusId);
        await appointmentsRepository.Received(1).UpdateAsync(fixture.Appointment, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T14_Handle_propagates_cancellation_token()
    {
        var fixture = CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "ATENDIDA");
        using var cts = new CancellationTokenSource();
        var command = new UpdateAppointmentStatusCommand(AppointmentId, AtendidaId, null);

        await sut.Handle(command, cts.Token);

        await appointmentsRepository.Received(1).GetByIdAsync(AppointmentId, cts.Token);
        await statusAppointmentsRepository.Received(1).GetByIdAsync(AgendadaId, cts.Token);
        await statusAppointmentsRepository.Received(1).GetByIdAsync(AtendidaId, cts.Token);
        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            cts.Token);
        await appointmentsRepository.Received(1).UpdateAsync(fixture.Appointment, cts.Token);
        await unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task STA_T19_Handle_does_not_save_when_transition_is_invalid()
    {
        CreateFixture(currentStatusName: "ATENDIDA", targetStatusName: "AGENDADA");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, AgendadaId, "motivo");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task STA_T20_Handle_does_not_save_when_cancelada_comment_exceeds_max_length()
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "CANCELADA");
        using var serviceProvider = BuildMediatorServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new UpdateAppointmentStatusCommand(
            AppointmentId,
            CanceladaId,
            new string('c', 101));

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task STA_T21_Handle_does_not_save_when_no_asistio_comment_is_missing(string? comment)
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "NO_ASISTIO");
        var command = new UpdateAppointmentStatusCommand(AppointmentId, NoAsistioId, comment);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private ServiceProvider BuildMediatorServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddLogging();
        services.AddSingleton(unitOfWork);

        return services.BuildServiceProvider();
    }

    private Fixture CreateFixture(string currentStatusName, string targetStatusName)
    {
        var currentStatusId = ResolveStatusId(currentStatusName);
        var targetStatusId = ResolveStatusId(targetStatusName);
        var appointment = CreateAppointment(currentStatusId);
        var currentStatus = CreateStatus(currentStatusName, currentStatusId);
        var targetStatus = CreateStatus(targetStatusName, targetStatusId);

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(currentStatusId, Arg.Any<CancellationToken>())
            .Returns(currentStatus);
        statusAppointmentsRepository.GetByIdAsync(targetStatusId, Arg.Any<CancellationToken>())
            .Returns(targetStatus);

        return new Fixture(appointment);
    }

    private static Guid ResolveStatusId(string statusName) =>
        statusName.ToUpperInvariant() switch
        {
            "AGENDADA" => AgendadaId,
            "ATENDIDA" => AtendidaId,
            "CANCELADA" => CanceladaId,
            "NO_ASISTIO" => NoAsistioId,
            _ => Guid.NewGuid()
        };

    private static Appointment CreateAppointment(Guid statusId) =>
        new(
            ClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            statusId,
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);

    private static StatusAppointment CreateStatus(string name, Guid id)
    {
        var status = new StatusAppointment(name, null);
        typeof(StatusAppointment)
            .GetProperty(nameof(StatusAppointment.Id))!
            .SetValue(status, id);
        return status;
    }

    private sealed record Fixture(Appointment Appointment);
}
