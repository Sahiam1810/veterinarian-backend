using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.VeterinarianAbsences.Entities;
using Domain.StatusAppointments.Entities;
using Application.StatusAppointments.Abstraction;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

// Política Etapa 5.1: perfil del dueño gana; sin perfil usa request normalizado.
public sealed class AppointmentRequesterPhonePolicyTests
{
    [Fact]
    public async Task Create_uses_profile_phone_when_request_differs()
    {
        var fixture = CreateStaffFixture(profilePhone: "3001234567");
        var command = fixture.Command with { RequesterPhoneNumber = "+57 301 999 8888" };

        Appointment? added = null;
        fixture.Appointments.AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                added = call.ArgAt<Appointment>(0);
                return Task.CompletedTask;
            });

        await fixture.CreateSut.Handle(command, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("3001234567", added!.RequesterPhoneNumber?.Value);
    }

    [Fact]
    public async Task Create_uses_request_phone_when_profile_phone_missing()
    {
        var fixture = CreateStaffFixture(profilePhone: null);
        var command = fixture.Command with { RequesterPhoneNumber = "+57 301 555 1234" };

        Appointment? added = null;
        fixture.Appointments.AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                added = call.ArgAt<Appointment>(0);
                return Task.CompletedTask;
            });

        await fixture.CreateSut.Handle(command, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("573015551234", added!.RequesterPhoneNumber?.Value);
    }

    [Fact]
    public async Task CreateMy_uses_profile_phone_when_request_differs()
    {
        // Reusa el fixture de CreateMy via handler real: se cubre en CreateMy tests actualizados.
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var clients = Substitute.For<IClientRepository>();

        var userId = Guid.NewGuid();
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);

        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.ClientsRepository.Returns(clients);
        clientPets.GetByIdAsync(clientPet.Id, Arg.Any<CancellationToken>()).Returns(clientPet);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        var phone = await AppointmentRequesterPhonePolicy.ResolveAsync(
            unitOfWork,
            clientPet.Id,
            "+57 301 999 8888",
            requirePhone: true,
            CancellationToken.None);

        Assert.Equal("3001234567", phone);
    }

    [Fact]
    public async Task Resolve_uses_request_when_profile_missing()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var clients = Substitute.For<IClientRepository>();

        var userId = Guid.NewGuid();
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: null);
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);

        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.ClientsRepository.Returns(clients);
        clientPets.GetByIdAsync(clientPet.Id, Arg.Any<CancellationToken>()).Returns(clientPet);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        var phone = await AppointmentRequesterPhonePolicy.ResolveAsync(
            unitOfWork,
            clientPet.Id,
            "+57 301 555 1234",
            requirePhone: true,
            CancellationToken.None);

        Assert.Equal("573015551234", phone);
    }

    [Fact]
    public async Task Update_realigns_requester_phone_to_profile_when_stale()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var availabilities = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
        var absences = Substitute.For<IVeterinarianAbsenceRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var clients = Substitute.For<IClientRepository>();
        var statuses = Substitute.For<IStatusAppointmentRepository>();

        var veterinarianId = Guid.NewGuid();
        var availability = new Availability(
            veterinarianId, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(18, 0));
        var userId = Guid.NewGuid();
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: "3001234567");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);

        var appointment = new Appointment(
            clientPet.Id,
            veterinarianId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            availability.Id,
            new DateTime(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 7, 14, 30, 0, DateTimeKind.Utc),
            notes: null,
            requesterPhoneNumber: "3009999999");

        unitOfWork.AppointmentsRepository.Returns(appointments);
        unitOfWork.AvailabilitiesRepository.Returns(availabilities);
        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.StatusAppointmentsRepository.Returns(statuses);
        statuses.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new StatusAppointment("AGENDADA", null));
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        clientPets.GetByIdAsync(clientPet.Id, Arg.Any<CancellationToken>()).Returns(clientPet);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
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

        var sut = new UpdateAppointmentCommandHandler(
            unitOfWork, absences, new FixedTimeProvider(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));
        await sut.Handle(
            new UpdateAppointmentCommand(
                appointment.Id,
                clientPet.Id,
                veterinarianId,
                appointment.ServiceId,
                appointment.StatusId,
                availability.Id,
                appointment.ScheduledStart,
                appointment.ScheduledEnd,
                "realineado"),
            CancellationToken.None);

        Assert.Equal("3001234567", appointment.RequesterPhoneNumber?.Value);
    }

    private static StaffFixture CreateStaffFixture(string? profilePhone)
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
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: profilePhone);
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);

        var command = new CreateAppointmentCommand(
            clientPet.Id, veterinarianId, Guid.NewGuid(), Guid.NewGuid(), availability.Id,
            new DateTime(2026, 9, 7, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 7, 14, 30, 0, DateTimeKind.Utc),
            null, "3000000000");

        unitOfWork.AppointmentsRepository.Returns(appointments);
        unitOfWork.AvailabilitiesRepository.Returns(availabilities);
        unitOfWork.ClientPetsRepository.Returns(clientPets);
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.ServicesRepository.Returns(services);
        clientPets.GetByIdAsync(clientPet.Id, Arg.Any<CancellationToken>()).Returns(clientPet);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        services.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>())
            .Returns(new Domain.Services.Entities.Service(Guid.NewGuid(), "Consulta general", 30, 50000m));
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

        return new StaffFixture(
            command,
            appointments,
            new CreateAppointmentCommandHandler(
                unitOfWork, absences, new FixedTimeProvider(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc))));
    }

    private sealed record StaffFixture(
        CreateAppointmentCommand Command,
        IAppointmentRepository Appointments,
        CreateAppointmentCommandHandler CreateSut);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
