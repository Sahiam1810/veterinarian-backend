using System.Security.Claims;
using Api.Appointments.Controllers;
using Api.Appointments.Dtos;
using Application.Appointments.UseCases;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class AppointmentVitalsApiTests
{
    private static readonly Guid ActorUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task VITALS_API_T01_patch_updates_and_returns_no_content()
    {
        sender.Send(Arg.Any<RecordAppointmentVitalsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        var request = new RecordAppointmentVitalsRequest(12.5m, 38.4m, 90, 26);

        var result = await controller.RecordVitals(AppointmentId, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await sender.Received(1).Send(
            Arg.Is<RecordAppointmentVitalsCommand>(c =>
                c.AppointmentId == AppointmentId
                && c.Weight == 12.5m
                && c.Temperature == 38.4m
                && c.HeartRate == 90
                && c.RespiratoryRate == 26),
            Arg.Any<CancellationToken>());
    }

    public static IEnumerable<object[]> InvalidValues()
    {
        yield return [0m, 38.4m, 90, 26];
        yield return [-1m, 38.4m, 90, 26];
        yield return [12.5m, 0m, 90, 26];
        yield return [12.5m, -1m, 90, 26];
        yield return [12.5m, 38.4m, 0, 26];
        yield return [12.5m, 38.4m, -1, 26];
        yield return [12.5m, 38.4m, 90, 0];
        yield return [12.5m, 38.4m, 90, -1];
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public async Task VITALS_API_T02_invalid_values_are_forwarded_to_sender(
        decimal? weight,
        decimal? temperature,
        int? heartRate,
        int? respiratoryRate)
    {
        sender.Send(Arg.Any<RecordAppointmentVitalsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        var request = new RecordAppointmentVitalsRequest(weight, temperature, heartRate, respiratoryRate);

        var result = await controller.RecordVitals(AppointmentId, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await sender.Received(1).Send(
            Arg.Is<RecordAppointmentVitalsCommand>(c =>
                c.AppointmentId == AppointmentId
                && c.Weight == weight
                && c.Temperature == temperature
                && c.HeartRate == heartRate
                && c.RespiratoryRate == respiratoryRate),
            Arg.Any<CancellationToken>());
    }

    private AppointmentsController CreateController()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", ActorUserId.ToString()),
            new Claim("role", "Auxiliar")
        ],
            authenticationType: "TestAuth");

        return new AppointmentsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }
}
