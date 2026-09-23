using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.MedicationOrders.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Infrastructure.MedicationOrders.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.MedicationOrders;

public sealed class MedicationOrderRepositoryGetPendingTests
{
    [Fact]
    public async Task GetPendingAsync_returns_only_pending_orders_with_pet_and_owner_loaded()
    {
        await using var context = CreateContext();

        var client = TestClients.Create(fullName: "Ana Dueña");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Firulais", 3, "M", 10m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);

        context.Set<ClientEntity>().Add(client);
        context.Set<PetEntity>().Add(pet);
        context.Set<ClientPetEntity>().Add(clientPet);

        var pendingOrder = new MedicationOrder(
            clientPet.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis 1") });

        var deliveredOrder = new MedicationOrder(
            clientPet.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis 2") });
        deliveredOrder.Complete();

        context.Set<MedicationOrder>().Add(pendingOrder);
        context.Set<MedicationOrder>().Add(deliveredOrder);
        await context.SaveChangesAsync();

        var repository = new MedicationOrderRepository(context);

        var result = (await repository.GetPendingAsync(CancellationToken.None)).ToList();

        var order = Assert.Single(result);
        Assert.Equal(pendingOrder.Id, order.Id);
        Assert.Equal("Pendiente", order.Status);
        Assert.NotNull(order.ClientPet);
        Assert.Equal("Firulais", order.ClientPet!.Pet.Name.Value);
        Assert.Equal("Ana Dueña", order.ClientPet.Client.FullName.Value);
    }

    private static VeterinaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VeterinaryDbContext(options);
    }
}
