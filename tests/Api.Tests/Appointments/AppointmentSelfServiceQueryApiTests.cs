using System.Reflection;
using Api.Appointments.Controllers;
using Api.Appointments.Mappings;
using Application.Common.Exceptions;
using Application.Security.Errors;
using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Veterinarians.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Tests.Appointments;

// Etapa 5.2b: GetMine/GetMineById Gone; ToResponse intacto.
public sealed class AppointmentSelfServiceQueryApiTests
{
    private readonly AppointmentsController controller = new(Substitute.For<ISender>());

    [Fact]
    public void ToResponse_includes_display_names_and_marks_oracle_dates_as_utc()
    {
        var appointment = CreateAppointmentWithDetails();
        var response = appointment.ToResponse();
        Assert.Equal("Luna", response.PetName);
        Assert.Equal("Dra. Ana Perez", response.VeterinarianName);
        Assert.Equal("Consulta general", response.ServiceName);
        Assert.Equal("AGENDADA", response.StatusName);
        Assert.Equal(DateTimeKind.Utc, response.ScheduledStart.Kind);
        Assert.Equal(DateTimeKind.Utc, response.ScheduledEnd.Kind);
    }

    [Theory]
    [InlineData(nameof(AppointmentsController.GetMine))]
    [InlineData(nameof(AppointmentsController.GetMineById))]
    public void Portal_mine_actions_are_allow_anonymous(string methodName)
    {
        var method = typeof(AppointmentsController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.DoesNotContain(
            method.GetCustomAttributes<AuthorizeAttribute>(inherit: true),
            a => a.Policy == "ClientOnly");
    }

    [Fact]
    public async Task GetMine_throws_ClientPortalGone()
    {
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.GetMine(cancellationToken: CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    [Fact]
    public async Task GetMineById_throws_ClientPortalGone()
    {
        var ex = await Assert.ThrowsAsync<GoneException>(() =>
            controller.GetMineById(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(ClientPortalErrors.Gone.Code, ex.Code);
    }

    private static Appointment CreateAppointmentWithDetails()
    {
        var client = new ClientEntity(Guid.NewGuid(), "1234567890", "Calle 1");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity(
            "Luna", 4, "F", 12m, null,
            species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(clientPet, pet);
        var veterinarian = new Veterinarian(Guid.NewGuid(), Guid.NewGuid(), "VET-001");
        typeof(Veterinarian).GetProperty(nameof(Veterinarian.User))!.SetValue(
            veterinarian,
            new UserEntity("Dra. Ana Perez", "ana@example.com", "hash", Guid.NewGuid()));
        var service = new Service(Guid.NewGuid(), "Consulta general", 30, 55000m);
        var status = new StatusAppointment("AGENDADA", null);
        var appointment = new Appointment(
            clientPet.Id, veterinarian.Id, service.Id, status.Id, Guid.NewGuid(),
            new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Unspecified),
            new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Unspecified),
            "Control preventivo");
        typeof(Appointment).GetProperty(nameof(Appointment.ClientPet))!.SetValue(appointment, clientPet);
        typeof(Appointment).GetProperty(nameof(Appointment.Veterinarian))!.SetValue(appointment, veterinarian);
        typeof(Appointment).GetProperty(nameof(Appointment.Service))!.SetValue(appointment, service);
        typeof(Appointment).GetProperty(nameof(Appointment.Status))!.SetValue(appointment, status);
        return appointment;
    }
}
