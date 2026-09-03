using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.Services.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Appointments;

public sealed class GetAppointmentBookingSlotsQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 14, 0, 0, TimeSpan.Zero);
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BookingSettings settings = new();

    [Fact]
    public async Task Handle_generates_service_sized_utc_slots_and_removes_occupied_time()
    {
        var fixture = ConfigureBookingData();
        var occupied = new Appointment(
            Guid.NewGuid(), fixture.Veterinarian.Id, fixture.Service.Id, Guid.NewGuid(),
            fixture.Availability.Id,
            new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 16, 0, 0, DateTimeKind.Utc), null);
        unitOfWork.AppointmentsRepository.GetScheduledOverlapsAsync(
                fixture.Veterinarian.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new[] { occupied });

        var result = await Handler().Handle(
            new GetAppointmentBookingSlotsQuery(
                fixture.AccountId, fixture.Veterinarian.Id, fixture.Service.Id,
                new DateOnly(2026, 9, 3)),
            CancellationToken.None);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 16, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 16, 30, 0, DateTimeKind.Utc),
            },
            result.Select(slot => slot.ScheduledStartUtc));
    }

    [Theory]
    [InlineData("2026-09-02")]
    [InlineData("2026-10-04")]
    public async Task Handle_rejects_dates_outside_booking_horizon(string date)
    {
        var fixture = ConfigureBookingData();

        var action = () => Handler().Handle(
            new GetAppointmentBookingSlotsQuery(
                fixture.AccountId, fixture.Veterinarian.Id, fixture.Service.Id,
                DateOnly.Parse(date)),
            CancellationToken.None);

        await Assert.ThrowsAnyAsync<Exception>(action);
    }

    private GetAppointmentBookingSlotsQueryHandler Handler() =>
        new(unitOfWork, settings, new FixedTimeProvider(Now));

    private BookingFixture ConfigureBookingData()
    {
        var accountId = Guid.NewGuid();
        var account = new UserAccountEntity(Guid.NewGuid(), "cliente", "cliente@test.com", "Activo");
        var client = new ClientEntity(account.UserId, "1234567890", null);
        var service = new Service(Guid.NewGuid(), "Consulta", 30, 50000m);
        var user = new UserEntity("Dra. Ana", "ana@test.com", "hash", Guid.NewGuid());
        var veterinarian = new Veterinarian(user.Id, Guid.NewGuid(), "VET001");
        typeof(Veterinarian).GetProperty(nameof(Veterinarian.User))!.SetValue(veterinarian, user);
        var availability = new Availability(
            veterinarian.Id, DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        unitOfWork.UserAccountsRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        unitOfWork.ClientsRepository.GetByUserIdAsync(account.UserId, Arg.Any<CancellationToken>())
            .Returns(client);
        unitOfWork.ServicesRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        unitOfWork.VeterinariansRepository.GetByIdAsync(
                veterinarian.Id, Arg.Any<CancellationToken>())
            .Returns(veterinarian);
        unitOfWork.AvailabilitiesRepository.GetAllByVeterinarianIdAsync(
                veterinarian.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { availability });
        unitOfWork.AppointmentsRepository.GetScheduledOverlapsAsync(
                veterinarian.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        return new BookingFixture(accountId, service, veterinarian, availability);
    }

    private sealed record BookingFixture(
        Guid AccountId, Service Service, Veterinarian Veterinarian, Availability Availability);

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
