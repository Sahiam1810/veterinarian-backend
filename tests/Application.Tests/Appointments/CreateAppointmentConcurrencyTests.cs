using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class CreateAppointmentConcurrencyTests
{
    [Fact]
    public async Task Handle_locks_availability_and_rechecks_overlap_before_insert()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var availabilities = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
        var absences = Substitute.For<IVeterinarianAbsenceRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var clients = Substitute.For<IClientRepository>();
        var services = Substitute.For<Application.Services.Abstraction.IServiceRepository>();
        var veterinarianId = Guid.NewGuid();
        var availability = new Availability(
            veterinarianId, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(18, 0));
        var userId = Guid.NewGuid();
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);
        var command = new CreateAppointmentCommand(
            clientPet.Id, veterinarianId, Guid.NewGuid(), Guid.NewGuid(), availability.Id,
            new DateTime(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 7, 14, 30, 0, DateTimeKind.Utc),
            null, "3001234567");

        unitOfWork.AppointmentsRepository.Returns(appointments);
        unitOfWork.AvailabilitiesRepository.Returns(availabilities);
        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.ServicesRepository.Returns(services);
        clientPets.GetByIdAsync(clientPet.Id, Arg.Any<CancellationToken>()).Returns(clientPet);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        services.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>())
            .Returns(new Service(Guid.NewGuid(), "Consulta general", 30, 50000m));
        absences.GetOverlappingAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VeterinarianAbsence>());
        availabilities.LockByIdAsync(availability.Id, Arg.Any<CancellationToken>())
            .Returns(availability);
        appointments.HasOverlappingAppointmentAsync(
                command.ClientPetId, veterinarianId, command.ScheduledStart, command.ScheduledEnd,
                null, Arg.Any<CancellationToken>())
            .Returns(true);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));

        var handler = new CreateAppointmentCommandHandler(unitOfWork, absences);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
        await availabilities.Received(1)
            .LockByIdAsync(availability.Id, Arg.Any<CancellationToken>());
        await appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}
