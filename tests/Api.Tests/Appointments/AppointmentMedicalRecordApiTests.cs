using System.Security.Claims;
using Api.Appointments.Controllers;
using Domain.Roles;
using Api.Appointments.Dtos;
using Application.MedicalRecords.UseCases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class AppointmentMedicalRecordApiTests
{
    private static readonly Guid ActorUserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DiagnosticId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public async Task MR_API_T01_controller_passes_enforce_true_for_veterinario()
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
    [InlineData("Auxiliar")]
    public async Task MR_API_T02_controller_passes_enforce_false_for_staff_roles(string role)
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
    public async Task MR_API_T03_controller_passes_enforce_false_for_superadmin()
    {
        await AssertEnforceFlagAsync(
            claims:
            [
                new Claim("sub", ActorUserAccountId.ToString()),
                new Claim("role_id", SystemRoles.SuperAdminId.ToString())
            ],
            expectedEnforce: false);
    }

    private async Task AssertEnforceFlagAsync(Claim[] claims, bool expectedEnforce)
    {
        sender.Send(Arg.Any<CreateAppointmentMedicalRecordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreateAppointmentMedicalRecordResult(
                Guid.NewGuid(),
                AppointmentId,
                Array.Empty<Guid>()));

        var controller = CreateController(claims);
        var request = new CreateAppointmentMedicalRecordRequest(
            DiagnosticId,
            "Síntomas",
            "Tratamiento",
            10m,
            38.5m,
            null);

        var result = await controller.CreateMedicalRecord(AppointmentId, request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        await sender.Received(1).Send(
            Arg.Is<CreateAppointmentMedicalRecordCommand>(c =>
                c.AppointmentId == AppointmentId
                && c.DiagnosticId == DiagnosticId
                && c.ActorUserAccountId == ActorUserAccountId
                && c.EnforceVeterinarianOwnership == expectedEnforce
                && c.Vaccinations == null),
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
