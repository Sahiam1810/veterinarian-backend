using System.Security.Claims;
using Api.Veterinarians.Controllers;
using Application.Veterinarians.UseCases;
using Domain.Veterinarians.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Veterinarians;

// QA-VET-01: cobertura a nivel controller para GET /api/veterinarians/me,
// mismo patrón que AppointmentOwnershipApiTests (ISender mockeado + ClaimsPrincipal
// fabricado, sin WebApplicationFactory/Oracle). No cubre 401/403 de la política
// [RequirePermission] real -- eso vive en el pipeline de autorización de ASP.NET,
// fuera del alcance de una invocación directa del controller; ver informe.
public sealed class VeterinariansMeApiTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task GetMe_returns_200_with_the_authenticated_veterinarian_profile()
    {
        var veterinarian = new Veterinarian(Guid.NewGuid(), Guid.NewGuid(), "LIC-100");
        sender.Send(Arg.Any<GetMyVeterinarianQuery>(), Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        var controller = CreateController([new Claim("sub", UserAccountId.ToString())]);

        var result = await controller.GetMe(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<Api.Veterinarians.Dtos.VeterinarianResponse>(okResult.Value);
        Assert.Equal(veterinarian.Id, response.Id);
        Assert.Equal(veterinarian.LicenseNumber, response.LicenseNumber);

        await sender.Received(1).Send(
            Arg.Is<GetMyVeterinarianQuery>(q => q.UserAccountId == UserAccountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_returns_401_when_the_subject_claim_is_missing()
    {
        var controller = CreateController([]);

        var result = await controller.GetMe(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        await sender.DidNotReceive().Send(Arg.Any<GetMyVeterinarianQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMe_returns_401_when_the_subject_claim_is_not_a_guid()
    {
        var controller = CreateController([new Claim("sub", "not-a-guid")]);

        var result = await controller.GetMe(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        await sender.DidNotReceive().Send(Arg.Any<GetMyVeterinarianQuery>(), Arg.Any<CancellationToken>());
    }

    private VeterinariansController CreateController(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var user = new ClaimsPrincipal(identity);

        return new VeterinariansController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }
}
