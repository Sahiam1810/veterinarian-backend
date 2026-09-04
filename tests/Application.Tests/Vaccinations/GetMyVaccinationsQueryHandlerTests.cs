using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.UserAccounts.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Vaccinations.UseCases;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.Vaccinations.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Vaccinations;

public sealed class GetMyVaccinationsQueryHandlerTests
{
    private static readonly Guid UserAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly IUserAccountsRepository userAccountsRepository = Substitute.For<IUserAccountsRepository>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly IVaccinationRepository vaccinationsRepository = Substitute.For<IVaccinationRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetMyVaccinationsQueryHandler sut;

    public GetMyVaccinationsQueryHandlerTests()
    {
        unitOfWork.UserAccountsRepository.Returns(userAccountsRepository);
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        unitOfWork.VaccinationsRepository.Returns(vaccinationsRepository);
        sut = new GetMyVaccinationsQueryHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_returns_only_vaccinations_for_the_authenticated_clients_pets()
    {
        var account = new UserAccountEntity(UserId, "cliente", "cliente@test.com", "Active");
        var client = new ClientEntity(UserId, "1234567890", "Calle 1");
        var pet = new PetEntity(
            "Firulais",
            3,
            "M",
            10m,
            null,
            new SpeciesEntity("Canino"),
            new RaceEntity("Mestizo"));
        var clientPet = new ClientPetEntity(client, pet, true);
        var expected = new[]
        {
            new Vaccination(
                clientPet.Id,
                Guid.NewGuid(),
                "Rabia",
                1,
                DateTime.UtcNow,
                DateTime.UtcNow.AddYears(1))
        };

        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        clientsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { clientPet });
        vaccinationsRepository.GetByClientPetIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == clientPet.Id),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await sut.Handle(
            new GetMyVaccinationsQuery(UserAccountId),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(expected[0].Id, result.Single().Id);
        await vaccinationsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_fails_closed_when_account_has_no_client_profile()
    {
        var account = new UserAccountEntity(UserId, "personal", "personal@test.com", "Active");
        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        clientsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyVaccinationsQuery(UserAccountId), CancellationToken.None));

        await vaccinationsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await clientPetsRepository.DidNotReceive()
            .GetByClientIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_fails_when_authenticated_account_does_not_exist()
    {
        userAccountsRepository.GetByIdAsync(UserAccountId, Arg.Any<CancellationToken>())
            .Returns((UserAccountEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new GetMyVaccinationsQuery(UserAccountId), CancellationToken.None));

        await clientsRepository.DidNotReceive()
            .GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await vaccinationsRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }
}
