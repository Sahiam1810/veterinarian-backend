using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Availabilities.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Application.VeterinarianAbsences.Abstraction;
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
using Domain.VeterinarianAbsences.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Appointments;

public sealed class RescheduleMyAppointmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_rejects_an_appointment_not_owned_by_the_authenticated_client()
    {
        var fixture = new Fixture();
        fixture.ClientPets.GetByClientIdAsync(
                fixture.Client.Id,
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_an_appointment_that_is_not_scheduled()
    {
        var fixture = new Fixture();
        fixture.Statuses.GetByIdAsync(
                fixture.Appointment.StatusId,
                Arg.Any<CancellationToken>())
            .Returns(new StatusAppointment("CANCELADA", null));

        await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
        await fixture.Appointments.Received(1).LockByIdAsync(
            fixture.Appointment.Id,
            Arg.Any<CancellationToken>());
        await fixture.Appointments.DidNotReceive().GetByIdAsync(
            fixture.Appointment.Id,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789012345678901")]
    public async Task Handle_rejects_an_invalid_contact_phone(string phone)
    {
        var fixture = new Fixture();
        var command = fixture.Command with { RequesterPhoneNumber = phone };

        await Assert.ThrowsAsync<BadRequestException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_null_contact_phone_as_bad_request()
    {
        var fixture = new Fixture();
        var command = fixture.Command with { RequesterPhoneNumber = null! };

        await Assert.ThrowsAsync<BadRequestException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().LockByIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_slot_that_became_occupied()
    {
        var fixture = new Fixture();
        fixture.Appointments.HasOverlappingAppointmentAsync(
                fixture.Appointment.ClientPetId,
                fixture.Appointment.VeterinarianId,
                fixture.Command.ScheduledStart,
                fixture.Command.ScheduledEnd,
                fixture.Appointment.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Availabilities.Received(1).LockByIdAsync(
            fixture.NewAvailability.Id,
            Arg.Any<CancellationToken>());
        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_time_outside_the_selected_availability()
    {
        var fixture = new Fixture();
        var command = fixture.Command with
        {
            ScheduledStart = new DateTime(2026, 9, 11, 13, 15, 0, DateTimeKind.Utc),
            ScheduledEnd = new DateTime(2026, 9, 11, 13, 45, 0, DateTimeKind.Utc),
        };

        await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_time_outside_the_booking_horizon()
    {
        var fixture = new Fixture();
        var command = fixture.Command with
        {
            ScheduledStart = new DateTime(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc),
            ScheduledEnd = new DateTime(2026, 11, 13, 15, 30, 0, DateTimeKind.Utc),
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));

        await fixture.Appointments.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_reschedules_and_updates_contact_phone_when_the_slot_is_free()
    {
        var fixture = new Fixture();

        await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Equal(fixture.NewAvailability.Id, fixture.Appointment.AvailabilityId);
        Assert.Equal(fixture.Command.ScheduledStart, fixture.Appointment.ScheduledStart);
        Assert.Equal(fixture.Command.ScheduledEnd, fixture.Appointment.ScheduledEnd);
        Assert.Equal("573158940150", fixture.Appointment.RequesterPhoneNumber?.Value);
        await fixture.Appointments.Received(1).HasOverlappingAppointmentAsync(
            fixture.Appointment.ClientPetId,
            fixture.Appointment.VeterinarianId,
            fixture.Command.ScheduledStart,
            fixture.Command.ScheduledEnd,
            fixture.Appointment.Id,
            Arg.Any<CancellationToken>());
        await fixture.Appointments.Received(1).UpdateAsync(
            fixture.Appointment,
            Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    private sealed class Fixture
    {
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public IAppointmentRepository Appointments { get; } =
            Substitute.For<IAppointmentRepository>();
        public IAvailabilityRepository Availabilities { get; } =
            Substitute.For<IAvailabilityRepository>();
        public IClientPetRepository ClientPets { get; } =
            Substitute.For<IClientPetRepository>();
        public IStatusAppointmentRepository Statuses { get; } =
            Substitute.For<IStatusAppointmentRepository>();
        public IVeterinarianAbsenceRepository Absences { get; } =
            Substitute.For<IVeterinarianAbsenceRepository>();

        public UserAccountEntity Account { get; }
        public ClientEntity Client { get; }
        public ClientPetEntity ClientPet { get; }
        public Appointment Appointment { get; }
        public Availability NewAvailability { get; }
        public Service Service { get; }
        public RescheduleMyAppointmentCommand Command { get; }
        public RescheduleMyAppointmentCommandHandler Sut { get; }

        public Fixture()
        {
            var userId = Guid.NewGuid();
            Account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Activo");
            Client = new ClientEntity(userId, "1095914051", null);
            var species = new SpeciesEntity("Perro");
            var pet = new PetEntity(
                "Pacho",
                4,
                "M",
                12m,
                null,
                species,
                new RaceEntity("Mestizo", species));
            ClientPet = new ClientPetEntity(Client, pet, true);
            var veterinarianId = Guid.NewGuid();
            var currentAvailabilityId = Guid.NewGuid();
            var scheduledStatus = new StatusAppointment("AGENDADA", null);
            Service = new Service(Guid.NewGuid(), "Consulta", 30, 50000m);
            Appointment = new Appointment(
                ClientPet.Id,
                veterinarianId,
                Service.Id,
                scheduledStatus.Id,
                currentAvailabilityId,
                new DateTime(2026, 9, 10, 16, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 10, 17, 0, 0, DateTimeKind.Utc),
                "Control",
                "3001234567");
            NewAvailability = new Availability(
                veterinarianId,
                DayOfWeek.Friday,
                new TimeOnly(8, 0),
                new TimeOnly(12, 0));
            Command = new RescheduleMyAppointmentCommand(
                Appointment.Id,
                Account.Id,
                NewAvailability.Id,
                new DateTime(2026, 9, 11, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 11, 15, 30, 0, DateTimeKind.Utc),
                "+57 315 894 0150",
                "Reagendada desde Telegram");

            UnitOfWork.AppointmentsRepository.Returns(Appointments);
            UnitOfWork.AvailabilitiesRepository.Returns(Availabilities);
            UnitOfWork.ClientPetsRepository.Returns(ClientPets);
            UnitOfWork.StatusAppointmentsRepository.Returns(Statuses);
            UnitOfWork.UserAccountsRepository.GetByIdAsync(
                    Account.Id,
                    Arg.Any<CancellationToken>())
                .Returns(Account);
            UnitOfWork.ClientsRepository.GetByUserIdAsync(
                    Account.UserId,
                    Arg.Any<CancellationToken>())
                .Returns(Client);
            ClientPets.GetByClientIdAsync(Client.Id, Arg.Any<CancellationToken>())
                .Returns(new[] { ClientPet });
            Appointments.LockByIdAsync(Appointment.Id, Arg.Any<CancellationToken>())
                .Returns(Appointment);
            Statuses.GetByIdAsync(Appointment.StatusId, Arg.Any<CancellationToken>())
                .Returns(scheduledStatus);
            Availabilities.LockByIdAsync(
                    NewAvailability.Id,
                    Arg.Any<CancellationToken>())
                .Returns(NewAvailability);
            Absences.GetOverlappingAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(Array.Empty<VeterinarianAbsence>());
            UnitOfWork.ServicesRepository.GetByIdAsync(
                    Service.Id,
                    Arg.Any<CancellationToken>())
                .Returns(Service);
            UnitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                    call.ArgAt<CancellationToken>(1)));
            Sut = new RescheduleMyAppointmentCommandHandler(
                UnitOfWork,
                Absences,
                new BookingSettings(),
                new FixedTimeProvider(
                    new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero)));
        }
    }

    private sealed class BookingSettings : IAppointmentBookingSettings
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
