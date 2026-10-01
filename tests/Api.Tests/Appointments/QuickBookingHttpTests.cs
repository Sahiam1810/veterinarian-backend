using System.Net;
using System.Net.Http.Json;
using Api.Tests.Support;
using Application.Appointments.UseCases;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

public sealed class QuickBookingHttpTests : IClassFixture<ModulePermissionsApiFactory>
{
    private readonly ModulePermissionsApiFactory factory;

    public QuickBookingHttpTests(ModulePermissionsApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task QuickBooking_With_CitasCreate_Returns_201_And_Forwards_Client_Source()
    {
        var appointmentId = Guid.NewGuid();
        factory.Sender.Send(Arg.Any<QuickBookingAppointmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(appointmentId);

        using var client = factory.CreateClientWithPermissions("Citas:Create");
        using var response = await client.PostAsJsonAsync("/api/appointments/quick-booking", new
        {
            clientPhoneNumber = "3001234567",
            clientFullName = "Ana Cliente",
            petName = "Luna",
            speciesId = Guid.NewGuid(),
            serviceId = Guid.NewGuid(),
            veterinarianId = Guid.NewGuid(),
            scheduledStart = new DateTime(2026, 9, 28, 10, 0, 0)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await factory.Sender.Received(1).Send(
            Arg.Is<QuickBookingAppointmentCommand>(command =>
                command.ClientId == null
                && command.ClientPhoneNumber == "3001234567"
                && command.ClientFullName == "Ana Cliente"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QuickBooking_Without_Token_Returns_401()
    {
        using var client = factory.CreateGuestClient();

        using var response = await client.PostAsJsonAsync(
            "/api/appointments/quick-booking",
            new { petName = "Luna" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task QuickBooking_Without_CitasCreate_Returns_403()
    {
        using var client = factory.CreateClientWithPermissions("Citas:View");

        using var response = await client.PostAsJsonAsync(
            "/api/appointments/quick-booking",
            new { petName = "Luna" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
