using Application.Common.Abstractions;
using Application.Services.Abstraction;
using Application.Services.UseCases;
using Domain.Services.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Services;

public sealed class GetAvailableServicesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyServicesProvidedByAvailableCatalogQuery()
    {
        var servicesRepository = Substitute.For<IServiceRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var expected = new[]
        {
            new Service(Guid.NewGuid(), "Consulta general", 30, 55_000m),
            new Service(Guid.NewGuid(), "Vacunación", 20, 45_000m)
        };
        unitOfWork.ServicesRepository.Returns(servicesRepository);
        servicesRepository.GetAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await new GetAvailableServicesQueryHandler(unitOfWork)
            .Handle(new GetAvailableServicesQuery(), CancellationToken.None);

        Assert.Equal(expected, result);
        await servicesRepository.DidNotReceive()
            .GetAllAsync(Arg.Any<CancellationToken>());
    }
}
