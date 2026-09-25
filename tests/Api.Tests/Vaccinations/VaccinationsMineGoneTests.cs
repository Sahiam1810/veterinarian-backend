using System.Reflection;
using Api.Vaccinations.Controllers;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.Vaccinations;

public sealed class VaccinationsMineGoneTests
{
    [Fact]
    public void GetMine_is_allow_anonymous()
    {
        var method = typeof(VaccinationsController).GetMethod(nameof(VaccinationsController.GetMine));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task GetMine_throws_ClientPortalGone()
    {
        var controller = new VaccinationsController(Substitute.For<ISender>());
        var ex = await Assert.ThrowsAsync<GoneException>(() => controller.GetMine(CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}