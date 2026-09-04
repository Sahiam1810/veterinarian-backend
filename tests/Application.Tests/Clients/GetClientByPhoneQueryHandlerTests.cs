using Application.Clients.Abstraction;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Clients;

// Tarea 2.2: lookup anónimo por teléfono (handler Application).
public sealed class GetClientByPhoneQueryHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly GetClientByPhoneQueryHandler sut;

    public GetClientByPhoneQueryHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        sut = new GetClientByPhoneQueryHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_returns_client_when_phone_matches()
    {
        var client = new ClientEntity(
            Guid.NewGuid(),
            "1234567890",
            "Calle 1",
            phoneNumber: "3001234567");

        clientsRepository.GetByPhoneAsync("3001234567", Arg.Any<CancellationToken>())
            .Returns(client);

        var result = await sut.Handle(
            new GetClientByPhoneQuery("3001234567"),
            CancellationToken.None);

        Assert.Same(client, result);
        await clientsRepository.Received(1).GetByPhoneAsync(
            "3001234567", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_not_found_when_phone_has_no_client()
    {
        clientsRepository.GetByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new GetClientByPhoneQuery("3009999999"), CancellationToken.None));
    }
}
