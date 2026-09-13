using Application.AppointmentStatusHistories.Abstraction;
using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class AppointmentsByIdentificationHandlerTests
{
    [Fact]
    public async Task List_by_unknown_cedula_returns_not_found()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        uow.ClientsRepository.Returns(clients);
        clients.GetByIdentificationNumberAsync("999", Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var handler = new GetAppointmentsByIdentificationQueryHandler(
            uow,
            TimeProvider.System);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetAppointmentsByIdentificationQuery("999"), CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_by_cedula_soft_cancels_owned_appointment()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var statuses = Substitute.For<IStatusAppointmentRepository>();
        var histories = Substitute.For<IAppointmentStatusHistoryRepository>();
        uow.ClientsRepository.Returns(clients);
        uow.ClientPetsRepository.Returns(clientPets);
        uow.AppointmentsRepository.Returns(appointments);
        uow.StatusAppointmentsRepository.Returns(statuses);
        uow.AppointmentStatusHistoriesRepository.Returns(histories);

        var userId = Guid.NewGuid();
        var client = new ClientEntity(userId, "1234567890", null);
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var pet = new PetEntity("Firulais", 3, "M", 10m, null, species, race);
        var ownership = new ClientPetEntity(client, pet, true);

        clients.GetByIdentificationNumberAsync("1234567890", Arg.Any<CancellationToken>())
            .Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns([ownership]);

        var agendada = new StatusAppointment("AGENDADA", null);
        var cancelada = new StatusAppointment("CANCELADA", null);
        var appointment = new Appointment(
            ownership.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            agendada.Id,
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddMinutes(30),
            null,
            "3001234567");

        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);
        statuses.GetByIdAsync(agendada.Id, Arg.Any<CancellationToken>())
            .Returns(agendada);
        statuses.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([agendada, cancelada]);

        var handler = new CancelAppointmentByIdentificationCommandHandler(uow);
        await handler.Handle(
            new CancelAppointmentByIdentificationCommand(appointment.Id, "1234567890", null),
            CancellationToken.None);

        await appointments.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await histories.Received(1).AddAsync(
            Arg.Any<Domain.AppointmentStatusHistories.Entities.AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(cancelada.Id, appointment.StatusId);
    }
}
