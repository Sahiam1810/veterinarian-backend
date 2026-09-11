using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Domain.Appointments.Entities;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class UpdateAppointmentCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OriginalStatusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid RequestedStatusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid VeterinarianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ServiceId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid AvailabilityId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly Application.Availabilities.Abstraction.IAvailabilityRepository availabilitiesRepository
        = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
    private readonly IVeterinarianAbsenceRepository absences
        = Substitute.For<IVeterinarianAbsenceRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IStatusAppointmentRepository statusAppointmentsRepository
        = Substitute.For<IStatusAppointmentRepository>();
    private readonly ClientEntity ownedClient;
    private readonly ClientPetEntity ownedClientPet;
    private readonly UpdateAppointmentCommandHandler sut;

    public UpdateAppointmentCommandHandlerTests()
    {
        var userId = Guid.NewGuid();
        ownedClient = new ClientEntity(userId, "1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        ownedClientPet = new ClientPetEntity(ownedClient, pet, true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Id))!
            .SetValue(ownedClientPet, ClientPetId);

        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.AvailabilitiesRepository.Returns(availabilitiesRepository);
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.StatusAppointmentsRepository.Returns(statusAppointmentsRepository);
        statusAppointmentsRepository.GetByIdAsync(OriginalStatusId, Arg.Any<CancellationToken>())
            .Returns(CreateStatus("AGENDADA", OriginalStatusId));
        clientPetsRepository.GetByIdAsync(ClientPetId, Arg.Any<CancellationToken>())
            .Returns(ownedClientPet);
        clientsRepository.GetByIdAsync(ownedClient.Id, Arg.Any<CancellationToken>())
            .Returns(ownedClient);
        absences.GetOverlappingAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VeterinarianAbsence>());
        availabilitiesRepository.LockByIdAsync(AvailabilityId, Arg.Any<CancellationToken>())
            .Returns(new Availability(
                VeterinarianId,
                DayOfWeek.Monday,
                new TimeOnly(8, 0),
                new TimeOnly(18, 0)));
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));
        sut = new UpdateAppointmentCommandHandler(unitOfWork, absences);
    }

    [Fact]
    public async Task STA_T15_Handle_ignores_StatusId_from_request_and_preserves_existing_status()
    {
        var appointment = new Appointment(
            ClientPetId,
            VeterinarianId,
            ServiceId,
            OriginalStatusId,
            AvailabilityId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "notas");

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var command = new UpdateAppointmentCommand(
            AppointmentId,
            ClientPetId,
            VeterinarianId,
            ServiceId,
            RequestedStatusId,
            AvailabilityId,
            appointment.ScheduledStart.AddHours(1),
            appointment.ScheduledEnd.AddHours(1),
            "actualizado");

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(OriginalStatusId, appointment.StatusId);
        Assert.Equal("actualizado", appointment.Notes);
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await availabilitiesRepository.Received(1)
            .LockByIdAsync(AvailabilityId, Arg.Any<CancellationToken>());
        await appointmentsRepository.Received(1).HasOverlappingAppointmentAsync(
            ClientPetId,
            VeterinarianId,
            command.ScheduledStart,
            command.ScheduledEnd,
            AppointmentId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_appointment_is_missing()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new UpdateAppointmentCommand(
            AppointmentId,
            ClientPetId,
            VeterinarianId,
            ServiceId,
            RequestedStatusId,
            AvailabilityId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
        await appointmentsRepository.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("ATENDIDA")]
    [InlineData("CANCELADA")]
    [InlineData("NO_ASISTIO")]
    public async Task Handle_rejects_reschedule_when_current_status_is_not_AGENDADA(
        string currentStatusName)
    {
        var appointment = new Appointment(
            ClientPetId,
            VeterinarianId,
            ServiceId,
            OriginalStatusId,
            AvailabilityId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "notas");

        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        statusAppointmentsRepository.GetByIdAsync(OriginalStatusId, Arg.Any<CancellationToken>())
            .Returns(CreateStatus(currentStatusName, OriginalStatusId));

        var command = new UpdateAppointmentCommand(
            AppointmentId,
            ClientPetId,
            VeterinarianId,
            ServiceId,
            RequestedStatusId,
            AvailabilityId,
            appointment.ScheduledStart.AddHours(1),
            appointment.ScheduledEnd.AddHours(1),
            "reprogramada");

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.Handle(command, CancellationToken.None));

        Assert.Equal(
            "No se puede reprogramar una cita que ya fue atendida, cancelada o marcada como no asistida.",
            ex.Message);
        await appointmentsRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    private static StatusAppointment CreateStatus(string name, Guid id)
    {
        var status = new StatusAppointment(name, null);
        typeof(StatusAppointment)
            .GetProperty(nameof(StatusAppointment.Id))!
            .SetValue(status, id);
        return status;
    }
}
