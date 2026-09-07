using System.Reflection;
using Api.Pets.Controllers;
using Api.Pets.Dtos;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Api.Tests.Pets;

// Etapa 5.2d: rutas pets/mine* responden Gone.
public sealed class PetsMineGoneTests
{
    private readonly PetsController controller = new(Substitute.For<IMediator>());

    [Theory]
    [InlineData(nameof(PetsController.GetMine))]
    [InlineData(nameof(PetsController.RegisterMine))]
    [InlineData(nameof(PetsController.UpdateMine))]
    public void Mine_actions_are_allow_anonymous(string methodName)
    {
        var method = typeof(PetsController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.DoesNotContain(
            method.GetCustomAttributes<AuthorizeAttribute>(true),
            a => a.Policy == Api.Common.Security.AuthorizationPolicies.ClientOnly);
    }

    [Fact]
    public async Task GetMine_throws_ClientPortalGone()
    {
        var ex = await Assert.ThrowsAsync<GoneException>(() => controller.GetMine(CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    [Fact]
    public async Task RegisterMine_throws_ClientPortalGone()
    {
        var dto = new CreateOwnedPetDto("Luna", 1, "F", 5m, null, Guid.NewGuid(), Guid.NewGuid());
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.RegisterMine(dto, CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    [Fact]
    public async Task UpdateMine_throws_ClientPortalGone()
    {
        var dto = new UpdateOwnedPetProfileDto(
            null, null, null, null, null, false, null, null, DateTime.UtcNow);
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.UpdateMine(Guid.NewGuid(), dto, CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}