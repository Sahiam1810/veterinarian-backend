using Domain.HospitalizationStays.Entities;
using Infrastructure.HospitalizationStays.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.HospitalizationStays;

public sealed class HospitalizationStayRepositoryTests
{
    private static VeterinaryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VeterinaryDbContext(options);
    }

    [Fact]
    public async Task GetAllActiveAsync_excludes_discharged_stays()
    {
        using var context = CreateDbContext();
        var repo = new HospitalizationStayRepository(context);

        var species = new Domain.Species.Entities.SpeciesEntity("Canino");
        var race = new Domain.Races.Entities.RaceEntity("Labrador", species);
        var pet = new Domain.Pets.Entities.PetEntity("Firulais", 2, "M", 10m, null, species, race);
        var client = new Domain.Clients.Entities.ClientEntity("Juan Perez", "juan@test.com", "12345", "5550000", null);
        var clientPet = new Domain.ClientsPets.Entities.ClientPetEntity(client, pet, true);

        var activeStay = new HospitalizationStay(clientPet.Id, null, Guid.NewGuid(), "Ingreso activo");
        var dischargedStay = new HospitalizationStay(clientPet.Id, null, Guid.NewGuid(), "Ingreso finalizado");
        dischargedStay.Discharge();

        await context.Set<Domain.Species.Entities.SpeciesEntity>().AddAsync(species);
        await context.Set<Domain.Races.Entities.RaceEntity>().AddAsync(race);
        await context.Set<Domain.Pets.Entities.PetEntity>().AddAsync(pet);
        await context.Set<Domain.Clients.Entities.ClientEntity>().AddAsync(client);
        await context.Set<Domain.ClientsPets.Entities.ClientPetEntity>().AddAsync(clientPet);
        await context.Set<HospitalizationStay>().AddRangeAsync(activeStay, dischargedStay);
        await context.SaveChangesAsync();

        var activeStays = await repo.GetAllActiveAsync();

        Assert.Single(activeStays);
        Assert.Contains(activeStays, s => s.Id == activeStay.Id);
        Assert.DoesNotContain(activeStays, s => s.Id == dischargedStay.Id);
    }
}
