using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Notifications.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.Veterinarians.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Appointments;

// Bug reportado: 1) la notificación de recordatorio mostraba la hora UTC cruda
// (8am Bogotá aparecía como 13:00) porque no se convertía a America/Bogota;
// 2) solo se notificaba al dueño de la mascota, nunca al veterinario asignado.
public sealed class GenerateUpcomingAppointmentRemindersCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRealtimeNotifier realtimeNotifier = Substitute.For<IRealtimeNotifier>();
    private readonly BookingSettings settings = new();

    [Fact]
    public async Task Handle_notifies_both_owner_and_veterinarian_with_the_local_Bogota_time()
    {
        var ownerUserId = Guid.NewGuid();
        var vetUserId = Guid.NewGuid();

        var species = new SpeciesEntity("Perro");
        var race = new RaceEntity("Labrador", species);
        var pet = new PetEntity("Milu", 5, "F", 10m, null, species, race);
        var client = new ClientEntity(ownerUserId, "1234567890", null);
        var clientPet = new ClientPetEntity(client, pet, true);
        SetProperty(clientPet, nameof(ClientPetEntity.Client), client);
        SetProperty(clientPet, nameof(ClientPetEntity.Pet), pet);

        var vetUser = new UserEntity("Dr. Carlos", "carlos@test.com", "hash", Guid.NewGuid());
        var veterinarian = new Veterinarian(vetUserId, Guid.NewGuid(), "VET002");
        SetProperty(veterinarian, nameof(Veterinarian.User), vetUser);

        // 13:00 UTC = 08:00 America/Bogota (UTC-5): la cita "de 8am" tal como se persiste.
        var scheduledStartUtc = new DateTime(2026, 9, 10, 13, 0, 0, DateTimeKind.Utc);
        var scheduledEndUtc = new DateTime(2026, 9, 10, 13, 30, 0, DateTimeKind.Utc);
        var appointment = new Appointment(
            clientPet.Id, veterinarian.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            scheduledStartUtc, scheduledEndUtc, null);
        SetProperty(appointment, nameof(Appointment.ClientPet), clientPet);
        SetProperty(appointment, nameof(Appointment.Veterinarian), veterinarian);

        unitOfWork.AppointmentsRepository.GetScheduledBetweenAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { appointment });
        unitOfWork.NotificationsRepository.GetNotifiedAppointmentIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var added = new List<Notification>();
        _ = unitOfWork.NotificationsRepository.AddAsync(
            Arg.Do<Notification>(n => added.Add(n)), Arg.Any<CancellationToken>());

        var handler = new GenerateUpcomingAppointmentRemindersCommandHandler(
            unitOfWork, realtimeNotifier, new FixedTimeProvider(Now), settings);

        var count = await handler.Handle(
            new GenerateUpcomingAppointmentRemindersCommand(TimeSpan.FromHours(24), []),
            CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(2, added.Count);

        var ownerReminder = Assert.Single(added, n => n.UserId == ownerUserId);
        Assert.Contains("08:00", ownerReminder.Message.Value);
        Assert.Contains("para Milu", ownerReminder.Message.Value);
        Assert.DoesNotContain("13:00", ownerReminder.Message.Value);

        var vetReminder = Assert.Single(added, n => n.UserId == vetUserId);
        Assert.Contains("08:00", vetReminder.Message.Value);
        Assert.Contains("con Milu", vetReminder.Message.Value);
        Assert.DoesNotContain("13:00", vetReminder.Message.Value);

        await realtimeNotifier.Received(2).NotifyUserAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

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
