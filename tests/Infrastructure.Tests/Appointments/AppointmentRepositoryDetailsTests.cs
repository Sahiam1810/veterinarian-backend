using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Services.Entities;
using Domain.Specialties.Entities;
using Domain.Species.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Users.Entities;
using Domain.Veterinarians.Entities;
using Infrastructure.Appointments.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Tests.Appointments;

public sealed class AppointmentRepositoryDetailsTests
{
    [Fact]
    public void Appointment_mapping_has_nullable_unique_booking_request_hash()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(Appointment));
        var property = entityType!.FindProperty(nameof(Appointment.BookingRequestKeyHash));

        Assert.NotNull(property);
        Assert.True(property.IsNullable);
        Assert.Equal(64, property.GetMaxLength());
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique && index.Properties.SequenceEqual(new[] { property }));
    }

    [Fact]
    public async Task GetByBookingRequestKeyHashAsync_returns_matching_appointment()
    {
        await using var context = CreateContext();
        var hash = new string('A', 64);
        var (appointment, _) = AddAppointmentGraph(context, hash);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await new AppointmentRepository(context)
            .GetByBookingRequestKeyHashAsync(hash, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(appointment.Id, loaded.Id);
    }

    [Fact]
    public async Task HasScheduledOverlapAsync_detects_pet_or_veterinarian_conflicts()
    {
        await using var context = CreateContext();
        var (appointment, clientPet) = AddAppointmentGraph(context);
        await context.SaveChangesAsync();

        var repository = new AppointmentRepository(context);
        var overlaps = await repository.HasScheduledOverlapAsync(
            clientPet.Id,
            Guid.NewGuid(),
            appointment.ScheduledStart.AddMinutes(10),
            appointment.ScheduledEnd.AddMinutes(10),
            CancellationToken.None);

        Assert.True(overlaps);
    }

    [Fact]
    public async Task GetByClientPetIdsAsync_loads_pet_and_veterinarian_names()
    {
        await using var context = CreateContext();
        var (appointment, clientPet) = AddAppointmentGraph(context);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new AppointmentRepository(context).GetByClientPetIdsAsync(
            new[] { clientPet.Id },
            CancellationToken.None);

        var loaded = Assert.Single(result);
        Assert.Equal("Luna", loaded.ClientPet!.Pet.Name.Value);
        Assert.Equal("Dra. Ana Pérez", loaded.Veterinarian!.User!.FullName);
        Assert.Equal(appointment.Id, loaded.Id);
    }

    [Fact]
    public async Task GetByIdAsync_loads_pet_and_veterinarian_names()
    {
        await using var context = CreateContext();
        var (appointment, _) = AddAppointmentGraph(context);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await new AppointmentRepository(context).GetByIdAsync(
            appointment.Id,
            CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Luna", loaded.ClientPet!.Pet.Name.Value);
        Assert.Equal("Dra. Ana Pérez", loaded.Veterinarian!.User!.FullName);
    }

    private static (Appointment Appointment, ClientPetEntity ClientPet) AddAppointmentGraph(
        VeterinaryDbContext context,
        string? bookingRequestKeyHash = null)
    {
        var clientUser = new UserEntity("Samuel Calderón", "samuel@example.com", "hash", Guid.NewGuid());
        var veterinarianUser = new UserEntity(
            "Dra. Ana Pérez", "ana@example.com", "hash", Guid.NewGuid());
        var client = new ClientEntity(clientUser.Id, "1234567890", "Calle 1");
        var pet = new PetEntity(
            "Luna",
            4,
            "F",
            12m,
            null,
            new SpeciesEntity("Canino"),
            new RaceEntity("Mestizo"));
        var clientPet = new ClientPetEntity(client, pet, true);
        var specialty = new SpecialtyEntity("Medicina general", null);
        var veterinarian = new Veterinarian(
            veterinarianUser.Id,
            specialty.Id,
            "VET-001");
        var service = new Service(Guid.NewGuid(), "Consulta general", 30, 55000m);
        var status = new StatusAppointment("AGENDADA", null);
        var availability = new Availability(
            veterinarian.Id,
            DayOfWeek.Wednesday,
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));
        var appointment = new Appointment(
            clientPet.Id,
            veterinarian.Id,
            service.Id,
            status.Id,
            availability.Id,
            new DateTime(2026, 9, 3, 15, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 3, 15, 30, 0, DateTimeKind.Utc),
            null,
            bookingRequestKeyHash: bookingRequestKeyHash);

        context.AddRange(
            clientUser,
            veterinarianUser,
            client,
            pet.Species,
            pet.Race,
            pet,
            clientPet,
            specialty,
            veterinarian,
            service,
            status,
            availability,
            appointment);
        return (appointment, clientPet);
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }
}
