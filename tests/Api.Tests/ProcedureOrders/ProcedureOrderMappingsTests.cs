using Api.ProcedureOrders.Mappings;
using Api.Tests.Support;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Xunit;

namespace Api.Tests.ProcedureOrders;

public sealed class ProcedureOrderMappingsTests
{
    [Fact]
    public void ToPendingDto_maps_pet_name_and_owner_name_from_client_pet_navigation()
    {
        var client = TestClients.Create(fullName: "Ana Dueña");
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Firulais", 3, "M", 10m, null, species, new RaceEntity("Mestizo", species));
        var clientPet = new ClientPetEntity(client, pet, true);
        Attach(clientPet, pet, client);

        var order = new ProcedureOrder(
            clientPet.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Examen 1") });
        typeof(ProcedureOrder).GetProperty(nameof(ProcedureOrder.ClientPet))!
            .SetValue(order, clientPet);

        var dto = order.ToPendingDto();

        Assert.Equal("Firulais", dto.PetName);
        Assert.Equal("Ana Dueña", dto.OwnerName);
        Assert.Equal("Pendiente", dto.Status);
        Assert.Single(dto.Items);
    }

    [Fact]
    public void ToPendingDto_falls_back_to_placeholder_when_client_pet_is_missing()
    {
        var order = new ProcedureOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Examen 1") });

        var dto = order.ToPendingDto();

        Assert.Equal("Desconocida", dto.PetName);
        Assert.Equal("Desconocido", dto.OwnerName);
    }

    // ClientPetEntity no cablea Client/Pet en su constructor (solo lee los Id) --
    // sin esto no hay forma de simular las navegaciones cargadas por EF.
    private static void Attach(ClientPetEntity clientPet, PetEntity pet, Domain.Clients.Entities.ClientEntity client)
    {
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(clientPet, pet);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Client))!.SetValue(clientPet, client);
    }
}
