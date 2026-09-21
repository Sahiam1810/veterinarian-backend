using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Pets.Abstraction;
using Application.Pets.UseCases;
using Application.Tests.Common;
using Domain.ClientsPets.Entities;
using Domain.ContactVerification.Enums;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Pets;

public sealed class QueryPetsByClaimProofHandlerTests
{
    [Fact]
    public async Task Handle_consumes_claim_proof_and_returns_pets_for_subject_user()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var client = TestClients.Create(userId, "1234567890", null);
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var pet = new PetEntity("Luna", 4, "F", 12.5m, "Sana", species, race);
        var relation = new ClientPetEntity(client, pet, true);

        var consume = Substitute.For<IConsumeContactVerificationProof>();
        consume.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumedContactVerificationProof(
                sessionId,
                ContactVerificationPurpose.Claim,
                userId,
                "hash"));

        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var pets = Substitute.For<IPetRepository>();
        uow.ClientsRepository.Returns(clients);
        uow.ClientPetsRepository.Returns(clientPets);
        uow.PetsRepository.Returns(pets);
        clients.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ClientPetEntity> { relation });
        pets.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<PetEntity> { pet });

        var sut = new QueryPetsByClaimProofHandler(consume, uow);

        var result = await sut.Handle(
            new QueryPetsByClaimProof(sessionId, "proof-token"),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Luna", result.First().Name);
        await consume.Received(1).ConsumeAsync(
            Arg.Is<ConsumeContactVerificationProof>(r =>
                r.SessionId == sessionId && r.Proof == "proof-token"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_non_claim_purpose()
    {
        var consume = Substitute.For<IConsumeContactVerificationProof>();
        consume.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumedContactVerificationProof(
                Guid.NewGuid(),
                ContactVerificationPurpose.Register,
                Guid.NewGuid(),
                "hash"));

        var sut = new QueryPetsByClaimProofHandler(consume, Substitute.For<IUnitOfWork>());

        var error = await Assert.ThrowsAsync<ContactVerificationException>(() =>
            sut.Handle(new QueryPetsByClaimProof(Guid.NewGuid(), "proof"), CancellationToken.None));

        Assert.Equal(ContactVerificationErrors.PurposeInvalid.Code, error.Code);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_client_missing()
    {
        var userId = Guid.NewGuid();
        var consume = Substitute.For<IConsumeContactVerificationProof>();
        consume.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), Arg.Any<CancellationToken>())
            .Returns(new ConsumedContactVerificationProof(
                Guid.NewGuid(),
                ContactVerificationPurpose.Claim,
                userId,
                "hash"));

        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        uow.ClientsRepository.Returns(clients);
        clients.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Domain.Clients.Entities.ClientEntity?)null);

        var sut = new QueryPetsByClaimProofHandler(consume, uow);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new QueryPetsByClaimProof(Guid.NewGuid(), "proof"), CancellationToken.None));
    }
}
