using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.HospitalizationStays.UseCases;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Hospitalization;

public sealed class HospitalizationAdmissionOptionsQueryHandlerTests
{
    [Fact]
    public async Task Returns_only_active_owner_relationships_with_the_expected_fields_and_order()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clientPetsRepository = Substitute.For<IClientPetRepository>();
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);

        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Labrador", species);
        var activeOwner = new ClientEntity("Ana Perez", "ana@test.com", "100000001", "3001000", null);
        var inactiveOwner = new ClientEntity("Bruno Perez", "bruno@test.com", "200000002", "3002000", null);
        inactiveOwner.Deactivate();

        var luna = new PetEntity("Luna", 2, "F", 8m, null, species, race);
        var max = new PetEntity("Max", 3, "M", 10m, null, species, race);
        var activeRelationship = CreateRelationship(activeOwner, luna);
        var inactiveRelationship = CreateRelationship(inactiveOwner, max);

        clientPetsRepository.GetAllWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { inactiveRelationship, activeRelationship });

        var result = await new GetHospitalizationAdmissionOptionsQueryHandler(unitOfWork)
            .Handle(new GetHospitalizationAdmissionOptionsQuery(), CancellationToken.None);

        var option = Assert.Single(result);
        Assert.Equal(activeRelationship.Id, option.ClientPetId);
        Assert.Equal("Luna", option.PetName);
        Assert.Equal("Ana Perez", option.OwnerName);
        await clientPetsRepository.Received(1).GetAllWithDetailsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Orders_options_by_pet_name_then_owner_name()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clientPetsRepository = Substitute.For<IClientPetRepository>();
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);

        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Labrador", species);
        var ownerZ = new ClientEntity("Zoe", "zoe@test.com", "300000003", "3003000", null);
        var ownerA = new ClientEntity("Ana", "ana2@test.com", "400000004", "3004000", null);
        var bella = new PetEntity("Bella", 2, "F", 8m, null, species, race);
        var luna = new PetEntity("Luna", 2, "F", 8m, null, species, race);

        clientPetsRepository.GetAllWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                CreateRelationship(ownerZ, luna),
                CreateRelationship(ownerA, bella),
            });

        var result = await new GetHospitalizationAdmissionOptionsQueryHandler(unitOfWork)
            .Handle(new GetHospitalizationAdmissionOptionsQuery(), CancellationToken.None);

        Assert.Equal(new[] { "Bella", "Luna" }, result.Select(option => option.PetName));
    }

    private static ClientPetEntity CreateRelationship(ClientEntity owner, PetEntity pet)
    {
        var relationship = new ClientPetEntity(owner, pet, isPrimaryOwner: true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Client))!.SetValue(relationship, owner);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(relationship, pet);
        return relationship;
    }
}
