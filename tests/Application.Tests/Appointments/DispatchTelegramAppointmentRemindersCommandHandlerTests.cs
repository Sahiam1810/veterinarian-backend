using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Notifications.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Telegram.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class DispatchTelegramAppointmentRemindersCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] Allowed = ["AGENDADA", "CONFIRMADA"];

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITelegramUnitOfWork telegramUnitOfWork = Substitute.For<ITelegramUnitOfWork>();
    private readonly ITelegramBotClient bot = Substitute.For<ITelegramBotClient>();
    private readonly ITelegramUserLinkRepository links = Substitute.For<ITelegramUserLinkRepository>();
    private readonly List<Notification> added = [];

    public DispatchTelegramAppointmentRemindersCommandHandlerTests()
    {
        telegramUnitOfWork.UserLinksRepository.Returns(links);
        unitOfWork.NotificationsRepository.GetNotifiedAppointmentIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        unitOfWork.NotificationsRepository.AddAsync(
            Arg.Do<Notification>(n => added.Add(n)),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        bot.SendTextAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
    }

    [Fact]
    public async Task Handle_sends_owner_telegram_with_bogota_time_and_ten_minute_arrival()
    {
        var ownerId = Guid.NewGuid();
        var appointment = CreateAppointment(ownerId, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(1));
        ArrangeAppointments(appointment);
        links.GetByPersonIdAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(TelegramUserLink.Create(ownerId, 99, 1001, Now.UtcDateTime));

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(1, result.Delivered);
        Assert.Equal(0, result.MissingLink);
        Assert.Equal(0, result.Deferred);
        await bot.Received(1).SendTextAsync(1001, Arg.Any<string>(), Arg.Any<CancellationToken>());
        var message = added.Single().Message.Value;
        Assert.Contains("Luna", message);
        Assert.Contains("08:00", message);
        Assert.Contains("11/09/2026", message);
        Assert.Contains("10 minutos antes", message);
        Assert.DoesNotContain("13:00", message);
        Assert.Equal("Recordatorio1h", added.Single().Type.Value);
        Assert.Equal("Enviado", added.Single().Status.Value);
        await unitOfWork.AppointmentsRepository.Received(1).GetScheduledBetweenAsync(
            Now.UtcDateTime.AddMinutes(50),
            Now.UtcDateTime.AddMinutes(70),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await unitOfWork.NotificationsRepository.Received(1).GetNotifiedAppointmentIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(appointment.Id)),
            "Recordatorio1h",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_records_missing_link_without_sending()
    {
        var ownerId = Guid.NewGuid();
        ArrangeAppointments(CreateAppointment(ownerId, "Luna", "CONFIRMADA", Now.UtcDateTime.AddHours(1)));
        links.GetByPersonIdAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns((TelegramUserLink?)null);

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(0, result.Delivered);
        Assert.Equal(1, result.MissingLink);
        await bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal("SinVinculo", added.Single().Status.Value);
        Assert.Equal("Recordatorio1h", added.Single().Type.Value);
    }

    [Fact]
    public async Task Handle_treats_revoked_link_as_missing()
    {
        var ownerId = Guid.NewGuid();
        ArrangeAppointments(CreateAppointment(ownerId, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(1)));
        var link = TelegramUserLink.Create(ownerId, 99, 1001, Now.UtcDateTime);
        link.Revoke(Now.UtcDateTime);
        links.GetByPersonIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(link);

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(1, result.MissingLink);
        await bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_skips_when_recordatorio1h_already_exists()
    {
        var ownerId = Guid.NewGuid();
        var appointment = CreateAppointment(ownerId, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(1));
        ArrangeAppointments(appointment);
        unitOfWork.NotificationsRepository.GetNotifiedAppointmentIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                "Recordatorio1h",
                Arg.Any<CancellationToken>())
            .Returns(new[] { appointment.Id });

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(new TelegramReminderDispatchResult(0, 0, 0), result);
        await bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_sends_even_when_24h_recordatorio_exists()
    {
        var ownerId = Guid.NewGuid();
        var appointment = CreateAppointment(ownerId, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(1));
        ArrangeAppointments(appointment);
        links.GetByPersonIdAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(TelegramUserLink.Create(ownerId, 99, 1001, Now.UtcDateTime));

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(1, result.Delivered);
        await unitOfWork.NotificationsRepository.DidNotReceive().GetNotifiedAppointmentIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            "Recordatorio",
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("ATENDIDA")]
    [InlineData("EN_PROGRESO")]
    [InlineData("NO_ASISTIO")]
    public async Task Handle_ignores_non_eligible_status(string status)
    {
        var ownerId = Guid.NewGuid();
        ArrangeAppointments(CreateAppointment(ownerId, "Luna", status, Now.UtcDateTime.AddHours(1)));

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(new TelegramReminderDispatchResult(0, 0, 0), result);
        await bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_ignores_appointment_outside_window()
    {
        var ownerId = Guid.NewGuid();
        ArrangeAppointments(CreateAppointment(ownerId, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(2)));

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(new TelegramReminderDispatchResult(0, 0, 0), result);
        await bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_does_not_persist_when_telegram_send_fails()
    {
        var ownerId = Guid.NewGuid();
        ArrangeAppointments(CreateAppointment(ownerId, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(1)));
        links.GetByPersonIdAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(TelegramUserLink.Create(ownerId, 99, 1001, Now.UtcDateTime));
        bot.SendTextAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TelegramDeliveryException());

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(0, result.Delivered);
        Assert.Equal(1, result.Deferred);
        Assert.Empty(added);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_continues_batch_when_one_send_fails()
    {
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var first = CreateAppointment(ownerA, "Luna", "AGENDADA", Now.UtcDateTime.AddHours(1));
        var second = CreateAppointment(ownerB, "Rocky", "CONFIRMADA", Now.UtcDateTime.AddMinutes(55));
        ArrangeAppointments(first, second);
        links.GetByPersonIdAsync(ownerA, Arg.Any<CancellationToken>())
            .Returns(TelegramUserLink.Create(ownerA, 1, 11, Now.UtcDateTime));
        links.GetByPersonIdAsync(ownerB, Arg.Any<CancellationToken>())
            .Returns(TelegramUserLink.Create(ownerB, 2, 22, Now.UtcDateTime));
        bot.SendTextAsync(11, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TelegramDeliveryException());

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal(1, result.Delivered);
        Assert.Equal(1, result.Deferred);
        Assert.Equal("Rocky", added.Single().Message.Value.Contains("Rocky") ? "Rocky" : added.Single().Message.Value);
        await bot.Received(1).SendTextAsync(22, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private DispatchTelegramAppointmentRemindersCommand Command() =>
        new(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(10), Allowed);

    private DispatchTelegramAppointmentRemindersCommandHandler Sut() =>
        new(
            unitOfWork,
            telegramUnitOfWork,
            bot,
            new FixedTimeProvider(Now),
            new BookingSettings(),
            Substitute.For<ILogger<DispatchTelegramAppointmentRemindersCommandHandler>>());

    private void ArrangeAppointments(params Appointment[] appointments) =>
        unitOfWork.AppointmentsRepository.GetScheduledBetweenAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(appointments);

    private static Appointment CreateAppointment(
        Guid ownerUserId,
        string petName,
        string statusName,
        DateTime scheduledStartUtc)
    {
        var species = new SpeciesEntity("Perro");
        var race = new RaceEntity("Labrador", species);
        var pet = new PetEntity(petName, 5, "F", 10m, null, species, race);
        var client = new ClientEntity(ownerUserId, "1234567890", null);
        var clientPet = new ClientPetEntity(client, pet, true);
        SetProperty(clientPet, nameof(ClientPetEntity.Client), client);
        SetProperty(clientPet, nameof(ClientPetEntity.Pet), pet);
        var status = new StatusAppointment(statusName, null);
        var appointment = new Appointment(
            clientPet.Id, Guid.NewGuid(), Guid.NewGuid(), status.Id, Guid.NewGuid(),
            scheduledStartUtc, scheduledStartUtc.AddMinutes(30), null);
        SetProperty(appointment, nameof(Appointment.ClientPet), clientPet);
        SetProperty(appointment, nameof(Appointment.Status), status);
        return appointment;
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
