using System.Security.Claims;
using Api.Appointments.Controllers;
using Api.Appointments.Dtos;
using Application.Appointments.UseCases;
using Domain.Appointments.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class AppointmentBookingApiTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task GetBookingOptions_uses_authenticated_account()
    {
        sender.Send(Arg.Any<GetAppointmentBookingOptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new AppointmentBookingOptionsResult([], [], [], true));
        var controller = CreateController();

        var result = await controller.GetBookingOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<GetAppointmentBookingOptionsQuery>(query =>
                query.UserAccountId == UserAccountId),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetBookingSlots_forwards_selected_veterinarian_service_and_date()
    {
        var veterinarianId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 10);
        sender.Send(Arg.Any<GetAppointmentBookingSlotsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppointmentBookingSlot>());
        var controller = CreateController();

        var result = await controller.GetBookingSlots(
            veterinarianId, serviceId, date, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<GetAppointmentBookingSlotsQuery>(query =>
                query.UserAccountId == UserAccountId
                && query.VeterinarianId == veterinarianId
                && query.ServiceId == serviceId
                && query.Date == date),
            CancellationToken.None);
    }

    [Fact]
    public async Task CreateMine_forwards_only_authenticated_identity_and_booking_contract()
    {
        var request = new CreateMyAppointmentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc),
            "Control",
            "3001234567");
        var appointment = new Appointment(
            Guid.NewGuid(), request.VeterinarianId, request.ServiceId, Guid.NewGuid(),
            Guid.NewGuid(), request.ScheduledStartUtc,
            request.ScheduledStartUtc.AddMinutes(30), request.Notes, request.RequesterPhoneNumber);
        sender.Send(Arg.Any<CreateMyAppointmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(appointment);
        var controller = CreateController();

        var result = await controller.CreateMine(
            request, "message-001", CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<CreateMyAppointmentCommand>(command =>
                command.UserAccountId == UserAccountId
                && command.PetId == request.PetId
                && command.VeterinarianId == request.VeterinarianId
                && command.ServiceId == request.ServiceId
                && command.ScheduledStartUtc == request.ScheduledStartUtc
                && command.IdempotencyKey == "message-001"),
            CancellationToken.None);
    }

    private AppointmentsController CreateController()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("sub", UserAccountId.ToString()) },
            "TestAuth");
        return new AppointmentsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                },
            },
        };
    }
}
