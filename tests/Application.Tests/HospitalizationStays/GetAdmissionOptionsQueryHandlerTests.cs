using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.HospitalizationStays.Dtos;
using Application.HospitalizationStays.UseCases;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.HospitalizationStays;

public sealed class GetAdmissionOptionsQueryHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly GetAdmissionOptionsQueryHandler handler;

    private static readonly SpeciesEntity DefaultSpecies = new("Canino");
    private static readonly RaceEntity DefaultRace = new("Labrador", DefaultSpecies);

    public GetAdmissionOptionsQueryHandlerTests()
    {
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        handler = new GetAdmissionOptionsQueryHandler(unitOfWork);
    }

    private static ClientPetEntity BuildClientPet(string petName, string ownerName)
    {
        var pet = new PetEntity(petName, 3, "M", 10m, null, DefaultSpecies, DefaultRace);
        var client = new ClientEntity(ownerName, $"{Guid.NewGuid():N}@vet.test", "12345678", "3001234567", null);
        var cp = new ClientPetEntity(client, pet, isPrimaryOwner: true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(cp, pet);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Client))!.SetValue(cp, client);
        return cp;
    }

    [Fact]
    public async Task ADMOPT_T01_returns_all_valid_client_pets_as_dtos()
    {
        var cp1 = BuildClientPet("Max", "Carlos Ruiz");
        var cp2 = BuildClientPet("Lola", "Ana García");

        clientPetsRepository
            .GetAllForAdmissionAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { cp1, cp2 });

        var result = await handler.Handle(new GetAdmissionOptionsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.ClientPetId == cp1.Id && x.PetName == "Max" && x.OwnerName == "Carlos Ruiz");
        Assert.Contains(result, x => x.ClientPetId == cp2.Id && x.PetName == "Lola" && x.OwnerName == "Ana García");
    }

    [Fact]
    public async Task ADMOPT_T02_dto_contains_only_clientPetId_petName_and_ownerName()
    {
        var cp = BuildClientPet("Firulais", "Pedro Soto");

        clientPetsRepository
            .GetAllForAdmissionAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { cp });

        var result = await handler.Handle(new GetAdmissionOptionsQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(cp.Id, dto.ClientPetId);
        Assert.Equal("Firulais", dto.PetName);
        Assert.Equal("Pedro Soto", dto.OwnerName);

        var props = typeof(HospitalizationAdmissionOptionDto)
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        var expectedProps = new[] { "ClientPetId", "OwnerName", "PetName" };
        Assert.Equal(expectedProps, props);
    }

    [Fact]
    public async Task ADMOPT_T03_returns_empty_list_when_no_client_pets_exist()
    {
        clientPetsRepository
            .GetAllForAdmissionAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());

        var result = await handler.Handle(new GetAdmissionOptionsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ADMOPT_T04_filters_out_client_pets_with_missing_nav_props()
    {
        var cpNoNavProps = new ClientPetEntity(
            new ClientEntity("Juan", "juan@test.com", "87654321", "3009876543", null),
            new PetEntity("Rocky", 2, "M", 5m, null, DefaultSpecies, DefaultRace),
            isPrimaryOwner: false);

        var cpValid = BuildClientPet("Luna", "María López");

        clientPetsRepository
            .GetAllForAdmissionAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { cpNoNavProps, cpValid });

        var result = await handler.Handle(new GetAdmissionOptionsQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("Luna", dto.PetName);
        Assert.Equal("María López", dto.OwnerName);
    }
}
