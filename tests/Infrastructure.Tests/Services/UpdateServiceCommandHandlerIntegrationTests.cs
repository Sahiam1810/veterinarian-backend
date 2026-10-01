using Application.Common.Abstractions;
using Application.Services.UseCases;
using Domain.Services.Entities;
using Domain.TypeServices.Entities;
using Infrastructure.Persistence;
using Infrastructure.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.Services;

public sealed class UpdateServiceCommandHandlerIntegrationTests
{
    [Fact]
    public async Task Handle_UpdatesTypeServiceIdAndReflectsChangesOnReload()
    {
        // 1. Setup in-memory EF Core database
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new VeterinaryDbContext(options);

        // 2. Seed initial catalog and service
        var typeServiceA = new TypeService("Medicina Preventiva", "Planes de vacunación");
        var typeServiceB = new TypeService("Cirugía", "Intervenciones quirúrgicas");
        context.AddRange(typeServiceA, typeServiceB);

        var service = new Service(typeServiceA.Id, "Consulta General", 30, 50000m, true);
        context.Add(service);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // 3. Setup real ServiceRepository with UnitOfWork wrapper
        var serviceRepository = new ServiceRepository(context);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ServicesRepository.Returns(serviceRepository);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo => await context.SaveChangesAsync(callInfo.Arg<CancellationToken>()));

        var handler = new UpdateServiceCommandHandler(unitOfWork);
        var command = new UpdateServiceCommand(
            service.Id,
            typeServiceB.Id,
            "Consulta General Editada",
            45,
            75000m,
            true);

        // 4. Act
        await handler.Handle(command, CancellationToken.None);
        context.ChangeTracker.Clear();

        // 5. Assert: reload service from repository and verify TypeServiceId and navigation property changed
        var reloadedService = await serviceRepository.GetByIdAsync(service.Id, CancellationToken.None);

        Assert.NotNull(reloadedService);
        Assert.Equal(typeServiceB.Id, reloadedService.TypeServiceId);
        Assert.NotNull(reloadedService.TypeService);
        Assert.Equal(typeServiceB.Id, reloadedService.TypeService.Id);
        Assert.Equal("Cirugía", reloadedService.TypeService.Name);
        Assert.Equal("Consulta General Editada", reloadedService.Name);
        Assert.Equal(45, reloadedService.DurationMinutes);
        Assert.Equal(75000m, reloadedService.Price);
    }
}
