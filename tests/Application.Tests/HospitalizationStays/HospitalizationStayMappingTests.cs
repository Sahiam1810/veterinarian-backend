using Application.HospitalizationStays.Mappings;
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
        Assert.Equal("Dra. Ana Lopez", dto.AdmittedByName);
        Assert.Equal("Activa", dto.Status);
        Assert.Null(dto.DischargedAt);
        Assert.Equal(stay.FechaIngreso, dto.AdmittedAt);
        Assert.Equal("Chequeo", dto.Motivo);
        Assert.False(dto.IsPaid);
        Assert.Null(dto.PaidAt);
    }

    [Fact]
    public void ToDto_maps_is_paid_and_paid_at_when_stay_is_paid()
    {
        var stay = new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Tratamiento");
        stay.RegisterPayment();

        var dto = stay.ToDto();

        Assert.True(dto.IsPaid);
        Assert.NotNull(dto.PaidAt);
        Assert.Equal(stay.PaidAt, dto.PaidAt);
    }


    [Fact]
    public void ToDto_handles_fallback_when_client_pet_is_null()
    {
        var stay = new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Observación sin relación");

        var dto = stay.ToDto("Dr. Pedro");

        Assert.Equal(stay.Id, dto.Id);
        Assert.Null(dto.PetName);
        Assert.Null(dto.OwnerName);
        Assert.Equal("Dr. Pedro", dto.AdmittedByName);
    }

    [Fact]
    public void ToDto_labels_a_discharged_stay_as_dada_de_alta()
    {
        var stay = new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Recuperado");
        stay.Discharge();

        var dto = stay.ToDto();

        Assert.Equal("Dada de alta", dto.Status);
        Assert.NotNull(dto.DischargedAt);
    }

    [Fact]
    public void Note_ToDto_maps_author_and_handover_names()
    {
        var stayId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var note = new HospitalizationNote(stayId, authorId, "Sin fiebre, come bien.", receiverId);

        var dto = note.ToDto("Dra. Ana", "Aux. Pedro");

        Assert.Equal(stayId, dto.StayId);
        Assert.Equal(authorId, dto.AuthorUserId);
        Assert.Equal("Dra. Ana", dto.AuthorName);
        Assert.Equal(receiverId, dto.HandedToUserId);
        Assert.Equal("Aux. Pedro", dto.HandedToName);
        Assert.Equal("Sin fiebre, come bien.", dto.Nota);
        Assert.Equal(note.FechaHora, dto.CreatedAt);
    }

    [Fact]
    public void Note_ToDto_without_handover_leaves_receiver_empty()
    {
        var note = new HospitalizationNote(Guid.NewGuid(), Guid.NewGuid(), "Turno tranquilo.", null);

        var dto = note.ToDto("Dra. Ana");

        Assert.Null(dto.HandedToUserId);
        Assert.Null(dto.HandedToName);
    }
}
