using System.Security.Claims;
using Api.Appointments.Controllers;
using Api.Appointments.Mappings;
using Application.Appointments.UseCases;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Users.Entities;
using Domain.Veterinarians.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Appointments;

public sealed class AppointmentSelfServiceQueryApiTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly ISender sender = Substitute.For<ISender>();

    [Fact]
    public void ToResponse_includes_display_names_and_marks_oracle_dates_as_utc()
    {
        var appointment = CreateAppointmentWithDetails();

        var response = appointment.ToResponse();

        Assert.Equal("Luna", response.PetName);
        Assert.Equal("Dra. Ana Pérez", response.VeterinarianName);
        Assert.Equal("Consulta general", response.ServiceName);
        Assert.Equal("AGENDADA", response.StatusName);
        Assert.Equal(DateTimeKind.Utc, response.ScheduledStart.Kind);
        Assert.Equal(DateTimeKind.Utc, response.ScheduledEnd.Kind);
    }

    [Fact]
    public async Task GetMine_forwards_authenticated_identity_and_scope()
    {
        sender.Send(Arg.Any<GetMyAppointmentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Appointment>());
        var controller = CreateController();

        await controller.GetMine(AppointmentQueryScope.Upcoming, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<GetMyAppointmentsQuery>(query =>
                query.UserAccountId == UserAccountId
                && query.Scope == AppointmentQueryScope.Upcoming),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetMineById_forwards_authenticated_identity_and_appointment_id()
    {
        var appointment = CreateAppointmentWithDetails();
        sender.Send(Arg.Any<GetMyAppointmentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(appointment);
        var controller = CreateController();

        var result = await controller.GetMineById(appointment.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await sender.Received(1).Send(
            Arg.Is<GetMyAppointmentByIdQuery>(query =>
                query.UserAccountId == UserAccountId
                && query.AppointmentId == appointment.Id),
            CancellationToken.None);
    }

    private AppointmentsController CreateController()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("sub", UserAccountId.ToString()) },
            "TestAuth");
        return new AppointmentsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static Appointment CreateAppointmentWithDetails()
    {
        var client = new ClientEntity(Guid.NewGuid(), "1234567890", "Calle 1");
        var pet = new PetEntity(
            "Luna",
            4,
            "F",
            12m,
            null,
            new SpeciesEntity("Canino"),
            new RaceEntity("Mestizo"));
        var clientPet = new ClientPetEntity(client, pet, true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(
            clientPet,
            pet);
        var veterinarian = new Veterinarian(Guid.NewGuid(), Guid.NewGuid(), "VET-001");
        typeof(Veterinarian).GetProperty(nameof(Veterinarian.User))!.SetValue(
            veterinarian,
            new UserEntity("Dra. Ana Pérez", "ana@example.com", "hash", Guid.NewGuid()));
        var service = new Service(Guid.NewGuid(), "Consulta general", 30, 55000m);
        var status = new StatusAppointment("AGENDADA", null);
        var appointment = new Appointment(
            clientPet.Id,
            veterinarian.Id,
            service.Id,
            status.Id,
            Guid.NewGuid(),
            new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Unspecified),
            new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Unspecified),
            "Control preventivo");
        typeof(Appointment).GetProperty(nameof(Appointment.ClientPet))!.SetValue(
            appointment,
            clientPet);
        typeof(Appointment).GetProperty(nameof(Appointment.Veterinarian))!.SetValue(
            appointment,
            veterinarian);
        typeof(Appointment).GetProperty(nameof(Appointment.Service))!.SetValue(
            appointment,
            service);
        typeof(Appointment).GetProperty(nameof(Appointment.Status))!.SetValue(
            appointment,
            status);
        return appointment;
    }
}
