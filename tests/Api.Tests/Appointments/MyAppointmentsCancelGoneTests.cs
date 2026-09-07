using System.Reflection;
using Api.Appointments.Controllers;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

// Etapa 5.2c: PATCH cancel JWT retirado; OTP no se toca aqui.
public sealed class MyAppointmentsCancelGoneTests
{
    [Fact]
    public void CancelMine_is_allow_anonymous_not_client_only()
    {
        var method = typeof(MyAppointmentsController).GetMethod(nameof(MyAppointmentsController.CancelMine));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.DoesNotContain(
            method.GetCustomAttributes<AuthorizeAttribute>(true),
            a => a.Policy == "ClientOnly");
    }

    [Fact]
    public async Task CancelMine_throws_ClientPortalGone()
    {
        var controller = new MyAppointmentsController(Substitute.For<ISender>());
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.CancelMine(Guid.NewGuid(), null, CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    [Theory]
    [InlineData(nameof(MyAppointmentsController.RequestCode))]
    [InlineData(nameof(MyAppointmentsController.ConfirmCode))]
    public void Otp_actions_remain_allow_anonymous(string methodName)
    {
        var method = typeof(MyAppointmentsController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}