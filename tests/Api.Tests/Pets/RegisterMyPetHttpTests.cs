using Api.Pets.Controllers;
using Api.Pets.Dtos;
using Application.Common.Exceptions;
using Application.Security.Errors;
using MediatR;
using NSubstitute;
using Xunit;

namespace Api.Tests.Pets;

// Legacy HTTP tests reemplazados por contrato Gone (Etapa 5.2d).
public sealed class RegisterMyPetHttpTests
{
    [Fact]
    public async Task RegisterMine_is_gone()
    {
        var controller = new PetsController(Substitute.For<IMediator>());
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.RegisterMine(
                new CreateOwnedPetDto("Luna", 1, "F", 5m, null, Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }
}