using System.Net;
using System.Net.Http.Json;
using Api.Appointments.Dtos;
using Api.Tests.Pets;
using Application.Appointments.UseCases;
using Domain.Appointments.Entities;
using MediatR;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

[Collection(TelegramAgentApiCollection.Name)]
public sealed class BotAppointmentsApiTests(TelegramAgentApiFactory factory)
{
    [Fact]
    public async Task Ordinary_authenticated_token_cannot_access_bot_appointments()
    {
        using var client = factory.CreateJwtClient(tokenUse: null);

        using var response = await client.GetAsync("/api/bot/appointments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delegated_token_lists_appointments_for_its_subject()
    {
        factory.Sender.Send(
                Arg.Is<GetMyAppointmentsQuery>(query =>
                    query.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    query.Scope == AppointmentQueryScope.Upcoming),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.GetAsync("/api/bot/appointments?scope=Upcoming");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Delegated_token_gets_booking_options_for_its_subject()
    {
        var result = new AppointmentBookingOptionsResult(
            Array.Empty<AppointmentBookingPet>(),
            Array.Empty<AppointmentBookingService>(),
            Array.Empty<AppointmentBookingVeterinarian>(),
            false);
        factory.Sender.Send(
                Arg.Is<GetAppointmentBookingOptionsQuery>(query =>
                    query.UserAccountId == TelegramAgentApiFactory.AccountId),
                Arg.Any<CancellationToken>())
            .Returns(result);
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.GetAsync("/api/bot/appointments/booking/options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AppointmentBookingOptionsResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Pets);
    }

    [Fact]
    public async Task Delegated_token_gets_slots_for_its_subject()
    {
        var veterinarianId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 10);
        factory.Sender.Send(
                Arg.Is<GetAppointmentBookingSlotsQuery>(query =>
                    query.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    query.VeterinarianId == veterinarianId &&
                    query.ServiceId == serviceId &&
                    query.Date == date),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppointmentBookingSlot>());
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.GetAsync(
            $"/api/bot/appointments/booking/slots?veterinarianId={veterinarianId}&serviceId={serviceId}&date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delegated_token_creates_appointment_for_its_subject()
    {
        var petId = Guid.NewGuid();
        var veterinarianId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduledStart = new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc);
        var appointment = new Appointment(
            Guid.NewGuid(),
            veterinarianId,
            serviceId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            scheduledStart,
            scheduledStart.AddMinutes(30),
            null,
            null,
            null,
            null);
        factory.Sender.Send(
                Arg.Is<CreateMyAppointmentCommand>(command =>
                    command.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    command.PetId == petId &&
                    command.IdempotencyKey == "telegram-update-123"),
                Arg.Any<CancellationToken>())
            .Returns(appointment);
        using var client = factory.CreateJwtClient("telegram_agent");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "telegram-update-123");

        using var response = await client.PostAsJsonAsync(
            "/api/bot/appointments",
            new CreateMyAppointmentRequest(
                petId,
                veterinarianId,
                serviceId,
                scheduledStart));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.Equal(appointment.Id, body?.Id);
    }

    [Fact]
    public async Task Delegated_token_cancels_appointment_for_its_subject()
    {
        var appointmentId = Guid.NewGuid();
        factory.Sender.Send(
                Arg.Is<CancelMyAppointmentCommand>(command =>
                    command.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    command.AppointmentId == appointmentId &&
                    command.Comment == "Ya no puedo asistir"),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.PatchAsJsonAsync(
            $"/api/bot/appointments/{appointmentId}/cancel",
            new CancelMyAppointmentRequest("Ya no puedo asistir"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delegated_token_reschedules_appointment_for_its_subject()
    {
        var appointmentId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();
        var scheduledStart = new DateTime(2026, 9, 11, 13, 0, 0, DateTimeKind.Utc);
        var scheduledEnd = scheduledStart.AddMinutes(30);
        factory.Sender.Send(
                Arg.Is<RescheduleMyAppointmentCommand>(command =>
                    command.AppointmentId == appointmentId &&
                    command.UserAccountId == TelegramAgentApiFactory.AccountId &&
                    command.AvailabilityId == availabilityId &&
                    command.ScheduledStart == scheduledStart &&
                    command.ScheduledEnd == scheduledEnd &&
                    command.RequesterPhoneNumber == "3158940150" &&
                    command.Notes == "Cambio solicitado por Telegram"),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using var client = factory.CreateJwtClient("telegram_agent");

        using var response = await client.PatchAsJsonAsync(
            $"/api/bot/appointments/{appointmentId}/reschedule",
            new RescheduleMyAppointmentRequest(
                availabilityId,
                scheduledStart,
                scheduledEnd,
                "3158940150",
                "Cambio solicitado por Telegram"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Ordinary_authenticated_token_cannot_reschedule_bot_appointment()
    {
        using var client = factory.CreateJwtClient(tokenUse: null);

        using var response = await client.PatchAsJsonAsync(
            $"/api/bot/appointments/{Guid.NewGuid()}/reschedule",
            new RescheduleMyAppointmentRequest(
                Guid.NewGuid(),
                new DateTime(2026, 9, 11, 13, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 11, 13, 30, 0, DateTimeKind.Utc),
                "3158940150"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
