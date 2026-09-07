using System.Reflection;
using Api.Appointments.Controllers;
using Api.Appointments.Dtos;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

// Etapa 5.2b: rutas portal JWT de AppointmentsController responden Gone.
public sealed class AppointmentBookingApiTests
{
    private readonly AppointmentsController controller = new(Substitute.For<ISender>());

    [Theory]
    [InlineData(nameof(AppointmentsController.GetBookingOptions))]
    [InlineData(nameof(AppointmentsController.GetBookingSlots))]
    [InlineData(nameof(AppointmentsController.CreateMine))]
    public void Portal_booking_actions_are_allow_anonymous(string methodName)
    {
        var method = typeof(AppointmentsController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.DoesNotContain(
            method.GetCustomAttributes<AuthorizeAttribute>(inherit: true),
            a => a.Policy == Api.Common.Security.AuthorizationPolicies.ClientOnly);
    }

    [Fact]
    public async Task GetBookingOptions_throws_ClientPortalGone()
    {
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.GetBookingOptions(CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    [Fact]
    public async Task GetBookingSlots_throws_ClientPortalGone()
    {
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.GetBookingSlots(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 10), CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    [Fact]
    public async Task CreateMine_throws_ClientPortalGone()
    {
        var request = new CreateMyAppointmentRequest(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc),
            "Control", "3001234567");
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.CreateMine(request, "message-001", CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}