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

    [Fact]
    public async Task Handle_uses_60_minute_service_duration_over_30_minute_availability_slot()
    {
        var fixture = ConfigureScheduleData();
        var service = RegisterService(60);

        var result = await Handler().Handle(
            new GetAvailableScheduleSlotsQuery(fixture.Veterinarian.Id, new DateOnly(2026, 9, 3), service.Id),
            CancellationToken.None);

        Assert.Equal(30, fixture.Availability.SlotDurationMinutes);
        Assert.Equal(
            new[]
            {
                new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 16, 0, 0, DateTimeKind.Utc),
            },
            result.Select(slot => slot.ScheduledStartUtc));
        Assert.All(result, slot =>
            Assert.Equal(slot.ScheduledStartUtc.AddMinutes(60), slot.ScheduledEndUtc));
    }

    [Fact]
    public async Task Handle_uses_45_minute_service_duration_over_30_minute_availability_slot()
    {
        var fixture = ConfigureScheduleData();
        var service = RegisterService(45);

        var result = await Handler().Handle(
            new GetAvailableScheduleSlotsQuery(fixture.Veterinarian.Id, new DateOnly(2026, 9, 3), service.Id),
            CancellationToken.None);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 14, 45, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 3, 16, 15, 0, DateTimeKind.Utc),
            },
            result.Select(slot => slot.ScheduledStartUtc));
        Assert.All(result, slot =>
            Assert.Equal(slot.ScheduledStartUtc.AddMinutes(45), slot.ScheduledEndUtc));
    }

    [Fact]
    public async Task Handle_excludes_start_30_minutes_after_an_existing_60_minute_appointment()
    {
        var fixture = ConfigureScheduleData();
        var occupied = new Appointment(
            Guid.NewGuid(),
            fixture.Veterinarian.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            fixture.Availability.Id,
            new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc),
            null);
        unitOfWork.AppointmentsRepository.GetScheduledOverlapsAsync(
                fixture.Veterinarian.Id, Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new[] { occupied });

        var result = await Handler().Handle(
            new GetAvailableScheduleSlotsQuery(fixture.Veterinarian.Id, new DateOnly(2026, 9, 3)),
            CancellationToken.None);

        var starts = result.Select(slot => slot.ScheduledStartUtc).ToArray();
        Assert.DoesNotContain(new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc), starts);
        Assert.DoesNotContain(new DateTime(2026, 9, 3, 14, 30, 0, DateTimeKind.Utc), starts);
        Assert.Equal(new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc), starts.First());
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

    private Service RegisterService(int durationMinutes)
    {
        var service = new Service(Guid.NewGuid(), $"Servicio {durationMinutes}", durationMinutes, 50000m);
        unitOfWork.ServicesRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        return service;
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
