using Api.Availabilities.Controllers;
using Application.Appointments.UseCases;
using Application.Availabilities.UseCase;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Availabilities;

public sealed class AvailabilitiesAvailableSlotsApiTests
{
    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task GetAvailableSlots_forwards_veterinarian_date_and_service()
    {
        var veterinarianId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 10);
        sender.Send(Arg.Any<GetAvailableScheduleSlotsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppointmentBookingSlot>());
        var controller = CreateController();

        var result = await controller.GetAvailableSlots(
            veterinarianId, date, serviceId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<GetAvailableScheduleSlotsQuery>(query =>
                query.VeterinarianId == veterinarianId
                && query.Date == date
                && query.ServiceId == serviceId),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetAvailableSlots_allows_omitting_service()
    {
        var veterinarianId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 10);
        sender.Send(Arg.Any<GetAvailableScheduleSlotsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppointmentBookingSlot>());
        var controller = CreateController();

        var result = await controller.GetAvailableSlots(
            veterinarianId, date, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<GetAvailableScheduleSlotsQuery>(query =>
                query.VeterinarianId == veterinarianId
                && query.Date == date
                && query.ServiceId == null),
            CancellationToken.None);
    }

    private AvailabilitiesController CreateController()
    {
        return new AvailabilitiesController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
    }
}
