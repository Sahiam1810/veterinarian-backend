using Application.Clients.Abstraction;
using Application.Clients.UseCases;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Clients;

public sealed class GetClientLookupQueryHandlerTests
{
    private readonly IClientRepository clientRepository = Substitute.For<IClientRepository>();
    private readonly GetClientLookupQueryHandler sut;

    public GetClientLookupQueryHandlerTests()
    {
        sut = new GetClientLookupQueryHandler(clientRepository);
    }

    [Fact]
    public async Task Handle_returns_client_when_lookup_matches()
    {
        var client = new ClientEntity(
            userId: Guid.NewGuid(),
            identificationNumber: "1234567890",
            address: "Calle Falsa 123",
            phoneNumber: "3001234567");

        clientRepository.GetByLookupAsync("1234567890", "3001234567", Arg.Any<CancellationToken>())
            .Returns(client);

        var query = new GetClientLookupQuery("1234567890", "3001234567");
        var result = await sut.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(client.Id, result.Id);
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_client_does_not_exist()
    {
        clientRepository.GetByLookupAsync("9999999999", null, Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var query = new GetClientLookupQuery("9999999999", null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(query, CancellationToken.None));
    }
}
