using Application.Appointments.Abstraction;
using Application.Availabilities.UseCase;
using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Services.Entities;
using Domain.VeterinarianAbsences.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Availabilities;

public sealed class GetAvailableScheduleSlotsQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 14, 0, 0, TimeSpan.Zero);
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IVeterinarianAbsenceRepository absences = Substitute.For<IVeterinarianAbsenceRepository>();
    private readonly BookingSettings settings = new();

    [Fact]
    public async Task Handle_uses_availability_slot_duration_when_service_is_omitted()
    {
        var fixture = ConfigureScheduleData();

        var result = await Handler().Handle(
            new GetAvailableScheduleSlotsQuery(fixture.Veterinarian.Id, new DateOnly(2026, 9, 3)),
            CancellationToken.None);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 16, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 16, 30, 0, DateTimeKind.Utc),
            },
            result.Select(slot => slot.ScheduledStartUtc));
        Assert.All(result, slot => Assert.Equal(fixture.Availability.Id, slot.AvailabilityId));
    }

    [Theory]
    [InlineData("2026-09-02")]
    [InlineData("2027-09-04")]
    public async Task Handle_rejects_dates_outside_staff_horizon(string date)
    {
        var fixture = ConfigureScheduleData();

        var action = () => Handler().Handle(
            new GetAvailableScheduleSlotsQuery(fixture.Veterinarian.Id, DateOnly.Parse(date)),
            CancellationToken.None);

        await Assert.ThrowsAnyAsync<Exception>(action);
    }

    private GetAvailableScheduleSlotsQueryHandler Handler() =>
        new(unitOfWork, absences, settings, new FixedTimeProvider(Now));

    private ScheduleFixture ConfigureScheduleData()
    {
        var service = new Service(Guid.NewGuid(), "Consulta", 30, 50000m);
        var user = new UserEntity("Dra. Ana", "ana@test.com", "hash", Guid.NewGuid());
        var veterinarian = new Veterinarian(user.Id, Guid.NewGuid(), "VET001");
        typeof(Veterinarian).GetProperty(nameof(Veterinarian.User))!.SetValue(veterinarian, user);
        var availability = new Availability(
            veterinarian.Id, DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        unitOfWork.VeterinariansRepository.GetByIdAsync(
                veterinarian.Id, Arg.Any<CancellationToken>())
            .Returns(veterinarian);
        unitOfWork.ServicesRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        unitOfWork.AvailabilitiesRepository.GetAllByVeterinarianIdAsync(
                veterinarian.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { availability });
        unitOfWork.AppointmentsRepository.GetScheduledOverlapsAsync(
                veterinarian.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        unitOfWork.AppointmentsRepository.GetScheduledRoomOverlapsAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        absences.GetOverlappingAsync(
                veterinarian.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VeterinarianAbsence>());
        return new ScheduleFixture(service, veterinarian, availability);
    }

    private sealed record ScheduleFixture(
        Service Service, Veterinarian Veterinarian, Availability Availability);

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
