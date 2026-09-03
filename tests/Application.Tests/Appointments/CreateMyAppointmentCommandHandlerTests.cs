using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using Domain.UserAccounts.Entities;
using Domain.Users.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Appointments;

public sealed class CreateMyAppointmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_creates_owned_appointment_with_authoritative_fields()
    {
        var fixture = new Fixture(withClientPhone: true);
        var result = await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Equal(fixture.ClientPet.Id, result.ClientPetId);
        Assert.Equal(fixture.Availability.Id, result.AvailabilityId);
        Assert.Equal(fixture.Status.Id, result.StatusId);
        Assert.Equal(new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Utc), result.ScheduledEnd);
        Assert.Equal("3001234567", result.RequesterPhoneNumber?.Value);
        Assert.NotNull(result.BookingRequestKeyHash);
        await fixture.Appointments.Received(1)
            .AddAsync(result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_uses_request_phone_only_when_profile_phone_is_missing()
    {
        var fixture = new Fixture(withClientPhone: false);
        var command = fixture.Command with { RequesterPhoneNumber = "+57 301 555 1234" };

        var result = await fixture.Sut.Handle(command, CancellationToken.None);

        Assert.Equal("573015551234", result.RequesterPhoneNumber?.Value);
    }

    [Fact]
    public async Task Handle_replays_existing_appointment_for_same_booking_key()
    {
        var fixture = new Fixture(withClientPhone: true);
        var existing = fixture.MatchingAppointment();
        fixture.Appointments.GetByBookingRequestKeyHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Same(existing, result);
        await fixture.Appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_replay_does_not_depend_on_a_later_profile_phone_change()
    {
        var fixture = new Fixture(withClientPhone: true);
        var existing = fixture.MatchingAppointment(phone: "3119876543");
        fixture.Appointments.GetByBookingRequestKeyHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Same(existing, result);
    }

    [Fact]
    public async Task Handle_rechecks_idempotency_after_waiting_for_availability_lock()
    {
        var fixture = new Fixture(withClientPhone: true);
        var existing = fixture.MatchingAppointment();
        fixture.Appointments.GetByBookingRequestKeyHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Appointment?)null, existing);

        var result = await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Same(existing, result);
        await fixture.Availabilities.Received(1)
            .LockByIdAsync(fixture.Availability.Id, Arg.Any<CancellationToken>());
        await fixture.Appointments.DidNotReceive()
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_reused_key_with_different_payload()
    {
        var fixture = new Fixture(withClientPhone: true);
        fixture.Appointments.GetByBookingRequestKeyHashAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fixture.DifferentAppointment());

        await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Sut.Handle(fixture.Command, CancellationToken.None));

    }

    [Fact]
    public async Task Handle_rechecks_overlap_after_lock_and_rejects_taken_slot()
    {
        var fixture = new Fixture(withClientPhone: true);
        fixture.Appointments.HasScheduledOverlapAsync(
                fixture.ClientPet.Id, fixture.Veterinarian.Id,
                fixture.Command.ScheduledStartUtc,
                fixture.Command.ScheduledStartUtc.AddMinutes(30),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Availabilities.Received(1)
            .LockByIdAsync(fixture.Availability.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_slot_that_crosses_the_local_calendar_day()
    {
        var fixture = new Fixture(withClientPhone: true);
        fixture.Availability.Update(
            fixture.Veterinarian.Id,
            DayOfWeek.Thursday,
            new TimeOnly(23, 15),
            new TimeOnly(23, 59),
            true);
        var command = fixture.Command with
        {
            ScheduledStartUtc = new DateTime(2026, 9, 4, 4, 45, 0, DateTimeKind.Utc),
        };

        await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Sut.Handle(command, CancellationToken.None));
    }

    private sealed class Fixture
    {
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public IAppointmentRepository Appointments { get; } = Substitute.For<IAppointmentRepository>();
        public Application.Availabilities.Abstraction.IAvailabilityRepository Availabilities { get; }
            = Substitute.For<Application.Availabilities.Abstraction.IAvailabilityRepository>();
        public ClientPetEntity ClientPet { get; }
        public Service Service { get; }
        public Veterinarian Veterinarian { get; }
        public Availability Availability { get; }
        public StatusAppointment Status { get; } = new("AGENDADA", null);
        public CreateMyAppointmentCommand Command { get; }
        public CreateMyAppointmentCommandHandler Sut { get; }

        public Fixture(bool withClientPhone)
        {
            var accountId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Activo");
            var client = new ClientEntity(
                userId, "1234567890", null,
                phoneNumber: withClientPhone ? "3001234567" : null);
            var pet = new PetEntity(
                "Luna", 4, "F", 12m, null,
                new SpeciesEntity("Canino"), new RaceEntity("Mestizo"));
            ClientPet = new ClientPetEntity(client, pet, true);
            Service = new Service(Guid.NewGuid(), "Consulta", 30, 50000m);
            var veterinarianUser = new UserEntity(
                "Dra. Ana", "ana@test.com", "hash", Guid.NewGuid());
            Veterinarian = new Veterinarian(veterinarianUser.Id, Guid.NewGuid(), "VET001");
            typeof(Veterinarian).GetProperty(nameof(Veterinarian.User))!
                .SetValue(Veterinarian, veterinarianUser);
            Availability = new Availability(
                Veterinarian.Id, DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(12, 0));
            Command = new CreateMyAppointmentCommand(
                accountId, pet.Id, Veterinarian.Id, Service.Id,
                new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc),
                "Control", null, "booking-message-001");

            UnitOfWork.AppointmentsRepository.Returns(Appointments);
            UnitOfWork.AvailabilitiesRepository.Returns(Availabilities);
            UnitOfWork.UserAccountsRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
                .Returns(account);
            UnitOfWork.ClientsRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
                .Returns(client);
            UnitOfWork.ClientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
                .Returns(new[] { ClientPet });
            UnitOfWork.ServicesRepository.GetByIdAsync(Service.Id, Arg.Any<CancellationToken>())
                .Returns(Service);
            UnitOfWork.VeterinariansRepository.GetByIdAsync(
                    Veterinarian.Id, Arg.Any<CancellationToken>())
                .Returns(Veterinarian);
            UnitOfWork.StatusAppointmentsRepository.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(new[] { Status });
            UnitOfWork.AvailabilitiesRepository.GetAllByVeterinarianIdAsync(
                    Veterinarian.Id, Arg.Any<CancellationToken>())
                .Returns(new[] { Availability });
            Availabilities.LockByIdAsync(Availability.Id, Arg.Any<CancellationToken>())
                .Returns(Availability);
            UnitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                    call.ArgAt<CancellationToken>(1)));
            Appointment? added = null;
            Appointments.AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    added = call.ArgAt<Appointment>(0);
                    return Task.CompletedTask;
                });
            Appointments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(_ => added);
            Sut = new CreateMyAppointmentCommandHandler(
                UnitOfWork, new Settings(), new FixedTimeProvider(Now));
        }

        public Appointment MatchingAppointment(string phone = "3001234567") => new(
            ClientPet.Id, Veterinarian.Id, Service.Id, Status.Id, Availability.Id,
            Command.ScheduledStartUtc, Command.ScheduledStartUtc.AddMinutes(30),
            Command.Notes, phone, new string('A', 64));

        public Appointment DifferentAppointment() => new(
            ClientPet.Id, Veterinarian.Id, Service.Id, Status.Id, Availability.Id,
            Command.ScheduledStartUtc, Command.ScheduledStartUtc.AddMinutes(30),
            "different", "3001234567", new string('A', 64));
    }

    private sealed class Settings : IAppointmentBookingSettings
    {
        public string TimeZoneId => "America/Bogota";
        public TimeSpan MinimumLeadTime => TimeSpan.FromMinutes(60);
        public int MaximumAdvanceDays => 30;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
