# Recordatorio Telegram de cita Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enviar al dueño un mensaje de Telegram ~1 hora antes de la cita pidiendo llegar 10 minutos antes, sin cambiar el recordatorio SignalR de 24 h ni tocar frontend o chatbot.

**Architecture:** Un caso de uso nuevo en Application despacha envíos con `ITelegramBotClient`. Un hosted service aparte en Infrastructure lo dispara cada 5 minutos sobre citas AGENDADA/CONFIRMADA en la ventana 50–70 minutos. El tipo `Recordatorio1h` no comparte idempotencia con `Recordatorio`.

**Tech Stack:** ASP.NET Core 10, MediatR, xUnit, NSubstitute, Telegram Bot API (`sendMessage`).

## Global Constraints

- No modificar `GenerateUpcomingAppointmentRemindersCommandHandler` ni `AppointmentReminderBackgroundService`.
- No crear endpoints HTTP, tablas, migraciones, pantallas ni código en `veterinarian-fronted` o `Huellitas_ChatBot`.
- Tipo de notificación exactamente `Recordatorio1h`; estados exactamente `Enviado` y `SinVinculo` (máximo 20 caracteres).
- Mensaje exactamente: `Recordatorio: {nombreMascota} tiene cita el {dd/MM/yyyy HH:mm}. Por favor llega 10 minutos antes.` con hora `America/Bogota`.
- Logs: `AppointmentId` y conteos; nunca chat id, cédula, teléfono ni el texto del mensaje.
- `PersonId` del vínculo Telegram es el `UserId` del dueño (`Client.UserId`).
- Persistencia `Enviado` solo después de un `SendTextAsync` exitoso, una cita a la vez.
- Una sola entrega Telegram por `AppointmentId` (cualquier `Recordatorio1h` bloquea).

## File map

- Create: `src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommand.cs`
- Create: `src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommandHandler.cs`
- Create: `src/Infrastructure/Appointments/Configuration/TelegramReminderOptions.cs`
- Create: `src/Infrastructure/Appointments/Configuration/TelegramReminderOptionsValidator.cs`
- Create: `src/Infrastructure/Appointments/BackgroundServices/TelegramAppointmentReminderBackgroundService.cs`
- Create: `tests/Application.Tests/Appointments/DispatchTelegramAppointmentRemindersCommandHandlerTests.cs`
- Create: `tests/Infrastructure.Tests/Appointments/TelegramReminderOptionsValidatorTests.cs`
- Modify: `src/Application/Application.csproj` (paquete `Microsoft.Extensions.Logging.Abstractions` 10.0.1)
- Modify: `src/Infrastructure/DependencyInjection.cs` (registro de opciones y hosted service, junto al de `ReminderOptions`)
- Modify: `README.md` (fila `TelegramReminders`)
- Modify: `.env.example` (claves `TelegramReminders__*`)

---

### Task 1: Caso de uso de envío Telegram

**Files:**
- Create: `src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommand.cs`
- Create: `src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommandHandler.cs`
- Create: `tests/Application.Tests/Appointments/DispatchTelegramAppointmentRemindersCommandHandlerTests.cs`
- Modify: `src/Application/Application.csproj`

**Interfaces:**
- Consumes: `IUnitOfWork.AppointmentsRepository.GetScheduledBetweenAsync`, `IUnitOfWork.NotificationsRepository.GetNotifiedAppointmentIdsAsync` / `AddAsync` / `SaveChangesAsync`, `ITelegramUnitOfWork.UserLinksRepository.GetByPersonIdAsync`, `ITelegramBotClient.SendTextAsync`, `TimeProvider`, `IAppointmentBookingSettings.TimeZoneId`
- Produces: `DispatchTelegramAppointmentRemindersCommand(TimeSpan Lead, TimeSpan Grace, IReadOnlyCollection<string> AllowedStatusNames) : IRequest<TelegramReminderDispatchResult>` and `TelegramReminderDispatchResult(int Delivered, int MissingLink, int Deferred)`

- [ ] **Step 1: Add logging abstractions to Application**

In `src/Application/Application.csproj`, inside the existing `ItemGroup` of `PackageReference`s, add:

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.1" />
```

- [ ] **Step 2: Write the failing tests**

Create `tests/Application.Tests/Appointments/DispatchTelegramAppointmentRemindersCommandHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 3: Run tests to verify they fail**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~DispatchTelegramAppointmentRemindersCommandHandlerTests
```

Expected: FAIL because `DispatchTelegramAppointmentRemindersCommand` / handler types do not exist.

- [ ] **Step 4: Write the command and handler**

Create `src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommand.cs`:

```csharp
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record DispatchTelegramAppointmentRemindersCommand(
    TimeSpan Lead,
    TimeSpan Grace,
    IReadOnlyCollection<string> AllowedStatusNames)
    : IRequest<TelegramReminderDispatchResult>;

public sealed record TelegramReminderDispatchResult(
    int Delivered,
    int MissingLink,
    int Deferred);
```

Create `src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommandHandler.cs`:

```csharp
using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Appointments.Entities;
using Domain.Notifications.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.UseCases;

public sealed class DispatchTelegramAppointmentRemindersCommandHandler(
    IUnitOfWork unitOfWork,
    ITelegramUnitOfWork telegramUnitOfWork,
    ITelegramBotClient botClient,
    TimeProvider timeProvider,
    IAppointmentBookingSettings bookingSettings,
    ILogger<DispatchTelegramAppointmentRemindersCommandHandler> logger)
    : IRequestHandler<DispatchTelegramAppointmentRemindersCommand, TelegramReminderDispatchResult>
{
    public const string ReminderType = "Recordatorio1h";
    public const string SentStatus = "Enviado";
    public const string MissingLinkStatus = "SinVinculo";

    public async Task<TelegramReminderDispatchResult> Handle(
        DispatchTelegramAppointmentRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var fromUtc = now.Add(request.Lead - request.Grace);
        var toUtc = now.Add(request.Lead + request.Grace);

        var appointments = await unitOfWork.AppointmentsRepository
            .GetScheduledBetweenAsync(fromUtc, toUtc, cancellationToken);

        appointments = appointments
            .Where(appointment =>
                appointment.ScheduledStart >= fromUtc &&
                appointment.ScheduledStart <= toUtc &&
                appointment.Status is not null &&
                request.AllowedStatusNames.Contains(
                    appointment.Status.Name,
                    StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (appointments.Count == 0)
        {
            return new TelegramReminderDispatchResult(0, 0, 0);
        }

        var notifiedIds = await unitOfWork.NotificationsRepository
            .GetNotifiedAppointmentIdsAsync(
                appointments.Select(appointment => appointment.Id).ToArray(),
                ReminderType,
                cancellationToken);

        var pending = appointments
            .Where(appointment => !notifiedIds.Contains(appointment.Id))
            .ToArray();

        var delivered = 0;
        var missingLink = 0;
        var deferred = 0;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(bookingSettings.TimeZoneId);

        foreach (var appointment in pending)
        {
            try
            {
                var outcome = await ProcessAsync(appointment, timeZone, now, cancellationToken);
                switch (outcome)
                {
                    case ProcessOutcome.Delivered:
                        delivered++;
                        break;
                    case ProcessOutcome.MissingLink:
                        missingLink++;
                        break;
                    case ProcessOutcome.Deferred:
                        deferred++;
                        break;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Fallo al procesar recordatorio Telegram. AppointmentId={AppointmentId}",
                    appointment.Id);
                deferred++;
            }
        }

        return new TelegramReminderDispatchResult(delivered, missingLink, deferred);
    }

    private async Task<ProcessOutcome> ProcessAsync(
        Appointment appointment,
        TimeZoneInfo timeZone,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var ownerUserId = appointment.ClientPet?.Client?.UserId;
        var petName = appointment.ClientPet?.Pet?.Name.Value;
        if (ownerUserId is null || ownerUserId == Guid.Empty || string.IsNullOrWhiteSpace(petName))
        {
            return ProcessOutcome.Deferred;
        }

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(appointment.ScheduledStart, timeZone);
        var message = BuildOwnerMessage(petName, localStart);

        var link = await telegramUnitOfWork.UserLinksRepository
            .GetByPersonIdAsync(ownerUserId.Value, cancellationToken);
        if (link is not { IsActive: true })
        {
            await PersistAsync(
                ownerUserId.Value,
                appointment.Id,
                message,
                now,
                MissingLinkStatus,
                cancellationToken);
            return ProcessOutcome.MissingLink;
        }

        try
        {
            await botClient.SendTextAsync(link.TelegramChatId, message, cancellationToken);
        }
        catch (TelegramDeliveryException exception)
        {
            logger.LogWarning(
                exception,
                "Fallo al enviar recordatorio Telegram. AppointmentId={AppointmentId}",
                appointment.Id);
            return ProcessOutcome.Deferred;
        }

        await PersistAsync(
            ownerUserId.Value,
            appointment.Id,
            message,
            now,
            SentStatus,
            cancellationToken);
        return ProcessOutcome.Delivered;
    }

    private async Task PersistAsync(
        Guid userId,
        Guid appointmentId,
        string message,
        DateTime now,
        string status,
        CancellationToken cancellationToken)
    {
        var notification = new Notification(
            userId,
            appointmentId,
            message,
            now,
            status,
            ReminderType);
        await unitOfWork.NotificationsRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildOwnerMessage(string petName, DateTime localStart) =>
        $"Recordatorio: {petName} tiene cita el {localStart:dd/MM/yyyy HH:mm}. Por favor llega 10 minutos antes.";

    private enum ProcessOutcome
    {
        Delivered,
        MissingLink,
        Deferred
    }
}
```

Do not inject or call `IRealtimeNotifier`.

- [ ] **Step 5: Run tests to verify they pass**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~DispatchTelegramAppointmentRemindersCommandHandlerTests
```

Expected: PASS all facts in that class.

Also run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~GenerateUpcomingAppointmentRemindersCommandHandlerTests
```

Expected: PASS. If that class fails, revert any accidental edit to the 24 h handler.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Application.csproj src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommand.cs src/Application/Appointments/UseCases/DispatchTelegramAppointmentRemindersCommandHandler.cs tests/Application.Tests/Appointments/DispatchTelegramAppointmentRemindersCommandHandlerTests.cs
git commit -m "feat: dispatch Telegram appointment reminders one hour ahead"
```

---

### Task 2: Worker, opciones y documentación

**Files:**
- Create: `src/Infrastructure/Appointments/Configuration/TelegramReminderOptions.cs`
- Create: `src/Infrastructure/Appointments/Configuration/TelegramReminderOptionsValidator.cs`
- Create: `src/Infrastructure/Appointments/BackgroundServices/TelegramAppointmentReminderBackgroundService.cs`
- Create: `tests/Infrastructure.Tests/Appointments/TelegramReminderOptionsValidatorTests.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Modify: `README.md`
- Modify: `.env.example`

**Interfaces:**
- Consumes: `DispatchTelegramAppointmentRemindersCommand`, `TelegramReminderDispatchResult`, `IOptions<TelegramReminderOptions>`, `IOptions<TelegramOptions>`, `ISender`
- Produces: hosted service that no-ops when `TelegramReminders:Enabled` or `Telegram:Enabled` is false; options section name `TelegramReminders`

- [ ] **Step 1: Write failing options tests**

Create `tests/Infrastructure.Tests/Appointments/TelegramReminderOptionsValidatorTests.cs`:

```csharp
using Infrastructure.Appointments.Configuration;
using Xunit;

namespace Infrastructure.Tests.Appointments;

public sealed class TelegramReminderOptionsValidatorTests
{
    [Fact]
    public void Disabled_options_skip_validation()
    {
        var options = new TelegramReminderOptions { Enabled = false, LeadMinutes = 0 };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Default_values_are_valid()
    {
        var result = new TelegramReminderOptionsValidator().Validate(null, new TelegramReminderOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Lead_must_be_between_1_and_1440()
    {
        var options = new TelegramReminderOptions { LeadMinutes = 0 };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Grace_cannot_exceed_lead()
    {
        var options = new TelegramReminderOptions { LeadMinutes = 60, GraceMinutes = 61 };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Allowed_statuses_are_required_when_enabled()
    {
        var options = new TelegramReminderOptions { AllowedStatusNames = [] };

        var result = new TelegramReminderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~TelegramReminderOptionsValidatorTests
```

Expected: FAIL because `TelegramReminderOptions` / validator do not exist.

- [ ] **Step 3: Implement options, validator, worker and registration**

Create `src/Infrastructure/Appointments/Configuration/TelegramReminderOptions.cs`:

```csharp
namespace Infrastructure.Appointments.Configuration;

public sealed class TelegramReminderOptions
{
    public const string SectionName = "TelegramReminders";

    public bool Enabled { get; init; } = true;
    public int LeadMinutes { get; init; } = 60;
    public int GraceMinutes { get; init; } = 10;
    public int PollIntervalMinutes { get; init; } = 5;
    public string[] AllowedStatusNames { get; init; } = ["AGENDADA", "CONFIRMADA"];
}
```

Create `src/Infrastructure/Appointments/Configuration/TelegramReminderOptionsValidator.cs`:

```csharp
using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.Configuration;

public sealed class TelegramReminderOptionsValidator : IValidateOptions<TelegramReminderOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramReminderOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.LeadMinutes < 1 || options.LeadMinutes > 1440)
        {
            failures.Add("TelegramReminders:LeadMinutes must be between 1 and 1440.");
        }

        if (options.GraceMinutes < 0 || options.GraceMinutes > options.LeadMinutes)
        {
            failures.Add("TelegramReminders:GraceMinutes must be between 0 and LeadMinutes.");
        }

        if (options.PollIntervalMinutes < 1 || options.PollIntervalMinutes > 1440)
        {
            failures.Add("TelegramReminders:PollIntervalMinutes must be between 1 and 1440.");
        }

        if (options.AllowedStatusNames is null ||
            options.AllowedStatusNames.Length == 0 ||
            options.AllowedStatusNames.All(string.IsNullOrWhiteSpace))
        {
            failures.Add("TelegramReminders:AllowedStatusNames must contain at least one status.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
```

Create `src/Infrastructure/Appointments/BackgroundServices/TelegramAppointmentReminderBackgroundService.cs`:

```csharp
using Application.Appointments.UseCases;
using Infrastructure.Telegram.Configuration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.BackgroundServices;

public sealed class TelegramAppointmentReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<TelegramReminderOptions> reminderOptions,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramAppointmentReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reminders = reminderOptions.Value;
        if (!reminders.Enabled || !telegramOptions.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(reminders.PollIntervalMinutes));

        do
        {
            try
            {
                await DispatchAsync(reminders, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Fallo al despachar recordatorios Telegram de citas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchAsync(
        TelegramReminderOptions reminders,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(
            new DispatchTelegramAppointmentRemindersCommand(
                TimeSpan.FromMinutes(reminders.LeadMinutes),
                TimeSpan.FromMinutes(reminders.GraceMinutes),
                reminders.AllowedStatusNames),
            cancellationToken);

        if (result.Delivered + result.MissingLink > 0)
        {
            logger.LogInformation(
                "Recordatorios Telegram: {Delivered} enviados, {MissingLink} sin vinculo.",
                result.Delivered,
                result.MissingLink);
        }

        if (result.Deferred > 0)
        {
            logger.LogWarning(
                "Quedaron {Deferred} recordatorios Telegram pendientes de reintento.",
                result.Deferred);
        }
    }
}
```

In `src/Infrastructure/DependencyInjection.cs`, immediately after the existing `ReminderOptions` registration and `AddHostedService<AppointmentReminderBackgroundService>()`, add:

```csharp
services.AddSingleton<IValidateOptions<TelegramReminderOptions>, TelegramReminderOptionsValidator>();
services.AddOptions<TelegramReminderOptions>()
    .Bind(configuration.GetSection(TelegramReminderOptions.SectionName))
    .ValidateOnStart();
services.AddHostedService<TelegramAppointmentReminderBackgroundService>();
```

Add `using Infrastructure.Appointments.BackgroundServices;` only if the existing `AppointmentReminderBackgroundService` using does not already cover the namespace (same namespace; no extra using). Keep `TelegramReminderOptions` using via `Infrastructure.Appointments.Configuration`, already imported for `ReminderOptions`.

- [ ] **Step 4: Document configuration**

In `README.md`, add this row to the business options table after `Reminders`:

```markdown
| `TelegramReminders` | Worker de aviso Telegram al dueño ~1 h antes; requiere `Telegram:Enabled=true`. Ventana 50–70 min, sondeo cada 5 min, estados `AGENDADA` y `CONFIRMADA`. |
```

In `.env.example`, immediately after the `Reminders__*` block, add:

```dotenv
# Aviso Telegram al dueno ~1 hora antes. Requiere Telegram__Enabled=true.
# No reemplaza Reminders (panel SignalR a 24 h).
TelegramReminders__Enabled=true
TelegramReminders__LeadMinutes=60
TelegramReminders__GraceMinutes=10
TelegramReminders__PollIntervalMinutes=5
TelegramReminders__AllowedStatusNames__0=AGENDADA
TelegramReminders__AllowedStatusNames__1=CONFIRMADA
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~TelegramReminderOptionsValidatorTests
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~DispatchTelegramAppointmentRemindersCommandHandlerTests|FullyQualifiedName~GenerateUpcomingAppointmentRemindersCommandHandlerTests
dotnet test veterinarian_backend.slnx --filter FullyQualifiedName~Appointments
```

Expected: PASS. The 24 h reminder tests still pass. No frontend or chatbot tests are required.

- [ ] **Step 6: Commit**

```powershell
git add src/Infrastructure/Appointments/Configuration/TelegramReminderOptions.cs src/Infrastructure/Appointments/Configuration/TelegramReminderOptionsValidator.cs src/Infrastructure/Appointments/BackgroundServices/TelegramAppointmentReminderBackgroundService.cs src/Infrastructure/DependencyInjection.cs tests/Infrastructure.Tests/Appointments/TelegramReminderOptionsValidatorTests.cs README.md .env.example
git commit -m "feat: run Telegram appointment reminder worker beside the 24h panel reminders"
```
