using Application.AppointmentStatusHistories.Abstraction;
using Application.AppointmentStatusHistories.UseCases;
using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Domain.AppointmentStatusHistories.Entities;
using Domain.Appointments.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.AppointmentStatusHistories;

public sealed class CreateAppointmentStatusHistoryCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AgendadaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AtendidaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid CanceladaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IStatusAppointmentRepository statusAppointmentsRepository = Substitute.For<IStatusAppointmentRepository>();
    private readonly IAppointmentStatusHistoryRepository appointmentStatusHistoriesRepository = Substitute.For<IAppointmentStatusHistoryRepository>();
    private readonly CreateAppointmentStatusHistoryCommandHandler sut;

    public CreateAppointmentStatusHistoryCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.StatusAppointmentsRepository.Returns(statusAppointmentsRepository);
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(appointmentStatusHistoriesRepository);
        sut = new CreateAppointmentStatusHistoryCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_creates_history_and_syncs_appointment_status_for_a_valid_transition()
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "ATENDIDA", out var appointment);
        var command = new CreateAppointmentStatusHistoryCommand(AppointmentId, AtendidaId, ClientPetId, null);

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(AtendidaId, appointment.StatusId);
        await appointmentStatusHistoriesRepository.Received(1).AddAsync(
            Arg.Is<AppointmentStatusHistory>(h =>
                h.AppointmentId == AppointmentId
                && h.StatusId == AtendidaId
                && h.ClientPetId == ClientPetId),
            Arg.Any<CancellationToken>());
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_transition_not_allowed_by_the_shared_rules()
    {
        CreateFixture(currentStatusName: "ATENDIDA", targetStatusName: "AGENDADA", out _);
        var command = new CreateAppointmentStatusHistoryCommand(AppointmentId, AgendadaId, ClientPetId, null);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await appointmentStatusHistoriesRepository.DidNotReceive().AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_CANCELADA_without_a_comment()
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "CANCELADA", out _);
        var command = new CreateAppointmentStatusHistoryCommand(AppointmentId, CanceladaId, ClientPetId, null);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_BadRequestException_when_the_pet_does_not_belong_to_the_appointment()
    {
        CreateFixture(currentStatusName: "AGENDADA", targetStatusName: "ATENDIDA", out _);
        var command = new CreateAppointmentStatusHistoryCommand(AppointmentId, AtendidaId, Guid.NewGuid(), null);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_appointment_is_missing()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);
        var command = new CreateAppointmentStatusHistoryCommand(AppointmentId, AtendidaId, ClientPetId, null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_target_status_is_missing()
    {
        var appointment = CreateAppointment(AgendadaId);
        var currentStatus = CreateStatus("AGENDADA", AgendadaId);
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>()).Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(AgendadaId, Arg.Any<CancellationToken>()).Returns(currentStatus);
        statusAppointmentsRepository.GetByIdAsync(AtendidaId, Arg.Any<CancellationToken>())
            .Returns((StatusAppointment?)null);

        var command = new CreateAppointmentStatusHistoryCommand(AppointmentId, AtendidaId, ClientPetId, null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    private void CreateFixture(string currentStatusName, string targetStatusName, out Appointment appointment)
    {
        var currentStatusId = ResolveStatusId(currentStatusName);
        var targetStatusId = ResolveStatusId(targetStatusName);
        appointment = CreateAppointment(currentStatusId);
        var currentStatus = CreateStatus(currentStatusName, currentStatusId);
        var targetStatus = CreateStatus(targetStatusName, targetStatusId);

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>()).Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(currentStatusId, Arg.Any<CancellationToken>()).Returns(currentStatus);
        statusAppointmentsRepository.GetByIdAsync(targetStatusId, Arg.Any<CancellationToken>()).Returns(targetStatus);
    }

    private static Guid ResolveStatusId(string statusName) =>
        statusName.ToUpperInvariant() switch
        {
            "AGENDADA" => AgendadaId,
            "ATENDIDA" => AtendidaId,
            "CANCELADA" => CanceladaId,
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
}
