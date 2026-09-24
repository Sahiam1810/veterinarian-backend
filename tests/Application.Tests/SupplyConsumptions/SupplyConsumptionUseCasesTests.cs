using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Supplies.Abstraction;
using Application.SupplyConsumptions.Abstraction;
using Application.SupplyConsumptions.UseCases;
using Domain.Supplies.Entities;
using Domain.SupplyConsumptions.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.SupplyConsumptions;

public sealed class SupplyConsumptionUseCasesTests
{
    [Fact]
    public async Task Register_successfully_deducts_stock_and_creates_consumption()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 50);
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(Guid.NewGuid(), null, userId, "Ingreso");

        var uow = CreateUnitOfWork(out var suppliesRepo, out var consumptionRepo, out var stayRepo);
        stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(stay);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);


        var command = new RegisterSupplyConsumptionCommand(stayId, supply.Id, 5, userId, "Uso en cirugia");

        var result = await new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(stayId, result.HospitalizationStayId);
        Assert.Equal(supply.Id, result.SupplyId);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(1500m, result.UnitPrice);
        Assert.Equal(7500m, result.Total);
        Assert.Equal(userId, result.RegisteredByUserId);
        Assert.Equal("Uso en cirugia", result.Notes);
        Assert.Equal(45, supply.Stock);

        await consumptionRepo.Received(1).AddAsync(Arg.Is<SupplyConsumption>(c => c.Total == 7500m), Arg.Any<CancellationToken>());
        await suppliesRepo.Received(1).UpdateAsync(supply, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_throws_NotFoundException_when_supply_does_not_exist()
    {
        var stayId = Guid.NewGuid();
        var supplyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var uow = CreateUnitOfWork(out var suppliesRepo, out _, out var stayRepo);
        stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>())
            .Returns(new Domain.HospitalizationStays.Entities.HospitalizationStay(Guid.NewGuid(), null, userId, "Ingreso"));
        suppliesRepo.GetByIdAsync(supplyId, Arg.Any<CancellationToken>()).Returns((Supply?)null);


        var command = new RegisterSupplyConsumptionCommand(stayId, supplyId, 5, userId);

        var action = () => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Register_throws_InvalidOperationException_when_supply_is_inactive()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 50, isActive: false);

        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(Guid.NewGuid(), null, userId, "Ingreso");
        var uow = CreateUnitOfWork(out var suppliesRepo, out _, out var stayRepo);
        stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(stay);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        var command = new RegisterSupplyConsumptionCommand(stayId, supply.Id, 5, userId);

        var action = () => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(action);
        Assert.Contains("inactivo", ex.Message);
    }

    [Fact]
    public async Task Register_throws_NotFoundException_when_stay_does_not_exist()
    {
        var stayId = Guid.NewGuid();
        var supplyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var uow = CreateUnitOfWork(out _, out _, out var stayRepo);
        stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>()).Returns((Domain.HospitalizationStays.Entities.HospitalizationStay?)null);

        var command = new RegisterSupplyConsumptionCommand(stayId, supplyId, 5, userId);

        await Assert.ThrowsAsync<NotFoundException>(() => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Register_throws_ConflictException_when_stay_is_discharged()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 50);
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(Guid.NewGuid(), null, userId, "Ingreso");
        stay.Discharge();

        var uow = CreateUnitOfWork(out var suppliesRepo, out _, out var stayRepo);
        stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(stay);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        var command = new RegisterSupplyConsumptionCommand(stayId, supply.Id, 5, userId);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None));
        Assert.Contains("dada de alta", ex.Message);
    }


    [Fact]
    public async Task Register_throws_InvalidOperationException_when_stock_insufficient()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 3);

        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(Guid.NewGuid(), null, userId, "Ingreso");
        var uow = CreateUnitOfWork(out var suppliesRepo, out _, out var stayRepo);
        stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(stay);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);


        var command = new RegisterSupplyConsumptionCommand(stayId, supply.Id, 5, userId);

        var action = () => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("Stock insuficiente.", ex.Message);
    }

    [Fact]
    public async Task GetByStayId_returns_consumptions()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supplyId = Guid.NewGuid();
        var list = new List<SupplyConsumption>
        {
            new(stayId, supplyId, 2, 1000m, userId, "Nota 1"),
            new(stayId, supplyId, 1, 1000m, userId, "Nota 2")
        };

        var uow = CreateUnitOfWork(out _, out var consumptionRepo, out _);
        consumptionRepo.GetByHospitalizationStayIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(list);

        var result = await new GetSupplyConsumptionsByStayIdQueryHandler(uow)
            .Handle(new GetSupplyConsumptionsByStayIdQuery(stayId), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetTotalByStayId_returns_total_cost()
    {
        var stayId = Guid.NewGuid();
        var uow = CreateUnitOfWork(out _, out var consumptionRepo, out _);

        consumptionRepo.GetTotalByHospitalizationStayIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(3500m);

        var total = await new GetSupplyConsumptionTotalByStayIdQueryHandler(uow)
            .Handle(new GetSupplyConsumptionTotalByStayIdQuery(stayId), CancellationToken.None);

        Assert.Equal(3500m, total);
    }

    private static IUnitOfWork CreateUnitOfWork(
        out ISupplyRepository suppliesRepo,
        out ISupplyConsumptionRepository consumptionRepo,
        out Application.HospitalizationStays.Abstraction.IHospitalizationStayRepository stayRepo)
    {
        var uow = Substitute.For<IUnitOfWork>();
        suppliesRepo = Substitute.For<ISupplyRepository>();
        consumptionRepo = Substitute.For<ISupplyConsumptionRepository>();
        stayRepo = Substitute.For<Application.HospitalizationStays.Abstraction.IHospitalizationStayRepository>();
        uow.SuppliesRepository.Returns(suppliesRepo);
        uow.SupplyConsumptionsRepository.Returns(consumptionRepo);
        uow.HospitalizationStaysRepository.Returns(stayRepo);
        return uow;
    }

}
