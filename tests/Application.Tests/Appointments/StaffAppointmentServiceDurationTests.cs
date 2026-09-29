using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using Domain.VeterinarianAbsences.Entities;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;
using Application.Tests.Common;

namespace Application.Tests.Appointments;

// El fin de una cita con servicio es inicio + Services.DurationMinutes; la franja
// generica de la disponibilidad (SlotDurationMinutes) no lo decide.
public sealed class StaffAppointmentServiceDurationTests
{
    // Lunes 09:00 hora Bogota.
    private static readonly DateTime StartUtc = new(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc);

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointments = Substitute.For<IAppointmentRepository>();
    private readonly Application.Availabilities.Abstraction.IAvailabilityRepository availabilities
        = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
    private readonly IVeterinarianAbsenceRepository absences = Substitute.For<IVeterinarianAbsenceRepository>();
    private readonly IClientPetRepository clientPets = Substitute.For<IClientPetRepository>();
    private readonly IClientRepository clients = Substitute.For<IClientRepository>();
    private readonly Application.Services.Abstraction.IServiceRepository services
        = Substitute.For<Application.Services.Abstraction.IServiceRepository>();
    private readonly Guid veterinarianId = Guid.NewGuid();
    private readonly Availability availability;
    private readonly ClientPetEntity clientPet;

    public StaffAppointmentServiceDurationTests()
    {
        availability = new Availability(
            veterinarianId,
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            slotDurationMinutes: 30);
        var client = TestClients.Create("1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        clientPet = new ClientPetEntity(client, pet, true);

        unitOfWork.AppointmentsRepository.Returns(appointments);
        unitOfWork.AvailabilitiesRepository.Returns(availabilities);
        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.ServicesRepository.Returns(services);
        clientPets.GetByIdAsync(clientPet.Id, Arg.Any<CancellationToken>()).Returns(clientPet);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        absences.GetOverlappingAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VeterinarianAbsence>());
        availabilities.LockByIdAsync(availability.Id, Arg.Any<CancellationToken>())
            .Returns(availability);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));
    }

    [Theory]
    [InlineData(60)]
    [InlineData(45)]
    public async Task Create_ignores_received_end_and_persists_start_plus_service_duration(int serviceMinutes)
    {
        var command = CreateCommand(serviceMinutes, receivedEnd: StartUtc.AddMinutes(30));
        Appointment? persisted = null;
        await appointments.AddAsync(
            Arg.Do<Appointment>(a => persisted = a), Arg.Any<CancellationToken>());

        var id = await CreateHandler().Handle(command, CancellationToken.None);

        var expectedEnd = StartUtc.AddMinutes(serviceMinutes);
        Assert.NotNull(persisted);
        Assert.Equal(persisted!.Id, id);
        Assert.Equal(StartUtc, persisted.ScheduledStart);
        Assert.Equal(expectedEnd, persisted.ScheduledEnd);
        await appointments.Received(1).HasOverlappingAppointmentAsync(
            clientPet.Id, veterinarianId, StartUtc, expectedEnd, null, Arg.Any<CancellationToken>());
        await appointments.DidNotReceive().HasOverlappingAppointmentAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), StartUtc.AddMinutes(30),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_with_60_minute_service_conflicts_with_appointment_starting_30_minutes_later()
    {
        // Solo existe choque si se revisa el rango completo 14:00-15:00 (hay una cita a las 14:30).
        var command = CreateCommand(60, receivedEnd: StartUtc.AddMinutes(30));
        appointments.HasOverlappingAppointmentAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Is<DateTime>(end => end > StartUtc.AddMinutes(30)),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => CreateHandler().Handle(command, CancellationToken.None));
        await appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_when_service_end_exceeds_configured_window_even_if_received_end_fits()
    {
        // Disponibilidad lunes 08:00-09:30 Bogota; servicio 60 desde 09:00 termina 10:00.
        availability.Update(
            veterinarianId,
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(9, 30),
            isActive: true,
            slotDurationMinutes: 30);
        var command = CreateCommand(60, receivedEnd: StartUtc.AddMinutes(30));

        await Assert.ThrowsAsync<ConflictException>(
            () => CreateHandler().Handle(command, CancellationToken.None));
        await appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public async Task Create_rejects_service_without_valid_duration(int durationMinutes)
    {
        var command = CreateCommand(durationMinutes, receivedEnd: StartUtc.AddMinutes(30));

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => CreateHandler().Handle(command, CancellationToken.None));

        Assert.Equal("El servicio no tiene una duración válida configurada.", ex.Message);
        await appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_validator_does_not_reject_received_end_and_checks_overlap_with_service_end()
    {
        var command = CreateCommand(60, receivedEnd: StartUtc.AddMinutes(-15));
        var validator = new CreateAppointmentCommandValidator(unitOfWork);

        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEnd);
        await appointments.Received(1).HasOverlappingAppointmentAsync(
            clientPet.Id, veterinarianId, StartUtc, StartUtc.AddMinutes(60),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_validator_does_not_reject_received_end_and_checks_overlap_with_service_end()
    {
        var serviceId = Guid.NewGuid();
        services.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(new Service(Guid.NewGuid(), "Consulta extendida", 60, 70000m));
        var appointmentId = Guid.NewGuid();
        var command = new UpdateAppointmentCommand(
            appointmentId,
            clientPet.Id,
            veterinarianId,
            serviceId,
            Guid.NewGuid(),
            availability.Id,
            StartUtc,
            StartUtc.AddMinutes(-15),
            null);
        var validator = new UpdateAppointmentCommandValidator(unitOfWork);

        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEnd);
        await appointments.Received(1).HasOverlappingAppointmentAsync(
            clientPet.Id, veterinarianId, StartUtc, StartUtc.AddMinutes(60),
            appointmentId, Arg.Any<CancellationToken>());
    }

    private CreateAppointmentCommand CreateCommand(int serviceMinutes, DateTime receivedEnd)
    {
        var serviceId = Guid.NewGuid();
        services.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(new Service(Guid.NewGuid(), "Servicio", serviceMinutes, 50000m));
        return new CreateAppointmentCommand(
            clientPet.Id,
            veterinarianId,
            serviceId,
            Guid.NewGuid(),
            availability.Id,
            StartUtc,
            receivedEnd,
            null,
            "3001234567");
    }

    private CreateAppointmentCommandHandler CreateHandler() =>
        new(unitOfWork, absences, new FixedTimeProvider(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
