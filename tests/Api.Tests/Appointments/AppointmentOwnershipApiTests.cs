using System.Security.Claims;
using System.Text.Json;
using Api.Appointments.Controllers;
using Api.Appointments.Dtos;
using Api.Common.Errors;
using Application.Appointments.UseCases;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class AppointmentOwnershipApiTests
{
    private static readonly Guid ActorUserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task OWN_ERR_T01_ForbiddenException_maps_to_403_via_GlobalExceptionHandler()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/appointments/" + AppointmentId;
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ForbiddenException("La cita no está asignada al veterinario autenticado."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(StatusCodes.Status403Forbidden, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", document.RootElement.GetProperty("error").GetString());
        Assert.Equal(
            "La cita no está asignada al veterinario autenticado.",
            document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task OWN_API_T01_controller_passes_enforce_true_for_veterinario()
    {
        await AssertEnforceFlagAsync(
            claims:
            [
                new Claim("sub", ActorUserAccountId.ToString()),
                new Claim("role", "Veterinario")
            ],
            expectedEnforce: true);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Recepcionista")]
    public async Task OWN_API_T02_controller_passes_enforce_false_for_admin_and_receptionist(string role)
    {
        await AssertEnforceFlagAsync(
            claims:
            [
                new Claim("sub", ActorUserAccountId.ToString()),
                new Claim("role", role)
            ],
            expectedEnforce: false);
    }

    [Fact]
    public async Task OWN_API_T03_controller_passes_enforce_false_for_superadmin()
    {
        await AssertEnforceFlagAsync(
            claims:
            [
                new Claim("sub", ActorUserAccountId.ToString()),
                new Claim("super_admin", "true")
            ],
            expectedEnforce: false);
    }

    private async Task AssertEnforceFlagAsync(Claim[] claims, bool expectedEnforce)
    {
        var appointment = new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);

        sender.Send(Arg.Any<GetAppointmentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(appointment);
        sender.Send(Arg.Any<UpdateAppointmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        sender.Send(Arg.Any<UpdateAppointmentStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var controller = CreateController(claims);

        await controller.GetById(AppointmentId, CancellationToken.None);
        await controller.Update(
            AppointmentId,
            new UpdateAppointmentRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                null),
            CancellationToken.None);
        await controller.UpdateStatus(
            AppointmentId,
            new UpdateAppointmentStatusRequest(Guid.NewGuid(), null),
            CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<GetAppointmentByIdQuery>(q =>
                q.Id == AppointmentId
                && q.ActorUserAccountId == ActorUserAccountId
                && q.EnforceVeterinarianOwnership == expectedEnforce),
            Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<UpdateAppointmentCommand>(c =>
                c.Id == AppointmentId
                && c.ActorUserAccountId == ActorUserAccountId
                && c.EnforceVeterinarianOwnership == expectedEnforce),
            Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<UpdateAppointmentStatusCommand>(c =>
                c.AppointmentId == AppointmentId
                && c.ActorUserAccountId == ActorUserAccountId
                && c.EnforceVeterinarianOwnership == expectedEnforce),
            Arg.Any<CancellationToken>());
    }

    private AppointmentsController CreateController(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        return new AppointmentsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }
}
