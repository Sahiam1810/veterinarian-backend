using Application.Clients.Abstraction;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Clients;

public sealed class GetClientByIdentificationQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_client_when_found()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        unitOfWork.ClientsRepository.Returns(clients);
        var client = new ClientEntity(Guid.NewGuid(), "1234567890", null, phoneNumber: "3001112233");
        clients.GetByIdentificationNumberAsync("1234567890", Arg.Any<CancellationToken>())
            .Returns(client);

        var sut = new GetClientByIdentificationQueryHandler(unitOfWork);
        var result = await sut.Handle(
            new GetClientByIdentificationQuery("1234567890"),
            CancellationToken.None);

        Assert.Equal(client.Id, result.Id);
        Assert.Equal("3001112233", result.PhoneNumber!.Value);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_missing()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        unitOfWork.ClientsRepository.Returns(clients);
        clients.GetByIdentificationNumberAsync("000", Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var sut = new GetClientByIdentificationQueryHandler(unitOfWork);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new GetClientByIdentificationQuery("000"), CancellationToken.None));
    }
}
