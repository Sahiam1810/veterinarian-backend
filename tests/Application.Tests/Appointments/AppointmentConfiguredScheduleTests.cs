using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using Domain.VeterinarianAbsences.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

// S30: agendar fuera del día/horario configurado debe rechazarse, no inventar disponibilidad.
public sealed class AppointmentConfiguredScheduleTests
{
    private const string ExpectedMessage =
        "El veterinario no tiene disponibilidad configurada para ese día u horario.";

    [Fact]
    public async Task Handle_rejects_appointment_on_a_day_without_configured_availability()
    {
        // Availability lunes 08:00-18:00; cita martes 09:00 hora Bogotá (14:00 UTC).
        var fixture = CreateFixture(
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            scheduledStartUtc: new DateTime(2026, 9, 8, 14, 0, 0, DateTimeKind.Utc),
            scheduledEndUtc: new DateTime(2026, 9, 8, 14, 30, 0, DateTimeKind.Utc));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Handler.Handle(fixture.Command, CancellationToken.None));

        Assert.Equal(ExpectedMessage, ex.Message);
        await fixture.Appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_appointment_outside_configured_time_range()
    {
        // Availability lunes 08:00-12:00; cita 14:00 hora Bogotá (19:00 UTC).
        var fixture = CreateFixture(
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            scheduledStartUtc: new DateTime(2026, 9, 7, 19, 0, 0, DateTimeKind.Utc),
            scheduledEndUtc: new DateTime(2026, 9, 7, 19, 30, 0, DateTimeKind.Utc));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Handler.Handle(fixture.Command, CancellationToken.None));

        Assert.Equal(ExpectedMessage, ex.Message);
        await fixture.Appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_accepts_appointment_inside_configured_day_and_time()
    {
        // Availability lunes 08:00-18:00; cita 09:00 hora Bogotá (14:00 UTC).
        var fixture = CreateFixture(
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            scheduledStartUtc: new DateTime(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc),
            scheduledEndUtc: new DateTime(2026, 9, 7, 14, 30, 0, DateTimeKind.Utc));

        await fixture.Handler.Handle(fixture.Command, CancellationToken.None);

        await fixture.Appointments.Received(1)
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    private static Fixture CreateFixture(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateTime scheduledStartUtc,
        DateTime scheduledEndUtc)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var availabilities = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
        var absences = Substitute.For<IVeterinarianAbsenceRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var clients = Substitute.For<IClientRepository>();
        var services = Substitute.For<Application.Services.Abstraction.IServiceRepository>();
        var veterinarianId = Guid.NewGuid();
        var availability = new Availability(veterinarianId, dayOfWeek, startTime, endTime);
        var userId = Guid.NewGuid();
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);
        var command = new CreateAppointmentCommand(
            clientPet.Id,
            veterinarianId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            availability.Id,
            scheduledStartUtc,
            scheduledEndUtc,
            null,
            "3001234567");

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
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));

        return new Fixture(
            new CreateAppointmentCommandHandler(unitOfWork, absences),
            command,
            appointments);
    }

    private sealed record Fixture(
        CreateAppointmentCommandHandler Handler,
        CreateAppointmentCommand Command,
        IAppointmentRepository Appointments);
}
