using Api.HospitalizationStays.Mappings;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.HospitalizationStays.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Xunit;

namespace Application.Tests.HospitalizationStays;

public sealed class HospitalizationStayMappingTests
{
    [Fact]
    public void ToDto_maps_correctly_when_client_pet_is_present()
    {
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Poodle", species);
        var pet = new PetEntity("Max", 3, "M", 8.5m, null, species, race);
        var client = new ClientEntity("Carlos Ruiz", "carlos@test.com", "123456", "5551234", null);
        var clientPet = new ClientPetEntity(client, pet, isPrimaryOwner: true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(clientPet, pet);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Client))!.SetValue(clientPet, client);

        var stay = new HospitalizationStay(clientPet.Id, null, Guid.NewGuid(), "Chequeo");
        // Reflectively set ClientPet or set via field/property
        typeof(HospitalizationStay).GetProperty(nameof(HospitalizationStay.ClientPet))!
            .SetValue(stay, clientPet);

        var dto = stay.ToDto("Dra. Ana Lopez");

        Assert.Equal(stay.Id, dto.Id);
        Assert.Equal(clientPet.Id, dto.ClientPetId);
        Assert.Equal("Max", dto.PetName);
        Assert.Equal("Carlos Ruiz", dto.OwnerName);
        Assert.Equal("Dra. Ana Lopez", dto.AdmittedByUserName);
        Assert.Equal(HospitalizationStayStatus.Activa, dto.Estado);
        Assert.Equal("Chequeo", dto.Motivo);
    }

    [Fact]
    public void ToDto_handles_fallback_when_client_pet_is_null()
    {
        var stay = new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Observación sin relación");

        var dto = stay.ToDto("Dr. Pedro");

        Assert.Equal(stay.Id, dto.Id);
        Assert.Null(dto.PetName);
        Assert.Null(dto.OwnerName);
        Assert.Equal("Dr. Pedro", dto.AdmittedByUserName);
    }
}
