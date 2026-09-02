using System.Security.Claims;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Pets.Controllers;
using Api.Pets.Dtos;
using Api.Races.Controllers;
using Api.Species.Controllers;
using Application.Pets.Models;
using Application.Pets.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Pets;

public sealed class RegisterMyPetHttpTests
{
    [Fact]
    public async Task Post_mine_derives_account_from_sub_and_returns_created_profile()
    {
        var accountId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        var profile = new OwnedPetProfile(
            Guid.NewGuid(), "Luna", 4, "F", 12.5m, null,
            speciesId, "Canino", raceId, "Mestizo", DateTime.UtcNow);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<RegisterMyPetCommand>(), Arg.Any<CancellationToken>())
            .Returns(profile);
        var controller = new PetsController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, accountId.ToString())],
                        "test"))
                }
            }
        };
        var request = new CreateOwnedPetDto(
            "Luna", 4, "F", 12.5m, null, speciesId, raceId);

        var response = await controller.RegisterMine(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        var body = Assert.IsType<OwnedPetProfileResponseDto>(created.Value);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("Luna", body.Name);
        await mediator.Received(1).Send(
            Arg.Is<RegisterMyPetCommand>(command =>
                command.UserAccountId == accountId
                && command.SpeciesId == speciesId
                && command.RaceId == raceId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Registration_contract_cannot_overpost_owner_identity()
    {
        var propertyNames = typeof(CreateOwnedPetDto).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("UserAccountId", propertyNames);
        Assert.DoesNotContain("PersonId", propertyNames);
        Assert.DoesNotContain("ClientId", propertyNames);
    }

    [Fact]
    public void Registration_requires_client_policy_and_catalog_reads_require_authentication()
    {
        var register = typeof(PetsController).GetMethod(nameof(PetsController.RegisterMine))!;
        var registrationPolicy = Assert.Single(
            register.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.ClientOnly, registrationPolicy.Policy);

        AssertAuthenticatedCatalogRead(typeof(SpeciesController));
        AssertAuthenticatedCatalogRead(typeof(RacesController));
    }

    private static void AssertAuthenticatedCatalogRead(Type controllerType)
    {
        var getAll = controllerType.GetMethod("GetAll")!;
        Assert.Single(getAll.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.Empty(getAll.GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true));
    }
}
