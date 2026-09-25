using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.HospitalizationStays.Abstraction;
using Application.Supplies.Abstraction;
using Application.SupplyConsumptions.Abstraction;
using Application.SupplyConsumptions.UseCases;
using Domain.HospitalizationStays.Entities;
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

        var uow = CreateUnitOfWork(out var suppliesRepo, out var consumptionRepo);
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

        var uow = CreateUnitOfWork(out var suppliesRepo, out _);
        suppliesRepo.GetByIdAsync(supplyId, Arg.Any<CancellationToken>()).Returns((Supply?)null);

        var command = new RegisterSupplyConsumptionCommand(stayId, supplyId, 5, userId);

        var action = () => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Register_throws_ConflictException_when_supply_is_inactive()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 50, isActive: false);

        var uow = CreateUnitOfWork(out var suppliesRepo, out _);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        var command = new RegisterSupplyConsumptionCommand(stayId, supply.Id, 5, userId);

        var action = () => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(action);
        Assert.Contains("inactivo", ex.Message);
    }

    [Fact]
    public async Task Register_throws_ConflictException_when_stock_insufficient_without_touching_stock()
    {
        var stayId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 3);

        var uow = CreateUnitOfWork(out var suppliesRepo, out _);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        var command = new RegisterSupplyConsumptionCommand(stayId, supply.Id, 5, userId);

        var action = () => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(action);
        Assert.StartsWith("Stock insuficiente", ex.Message);
        Assert.Equal(3, supply.Stock);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var uow = CreateUnitOfWork(out _, out var consumptionRepo);
        consumptionRepo.GetByHospitalizationStayIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(list);

        var result = await new GetSupplyConsumptionsByStayIdQueryHandler(uow)
            .Handle(new GetSupplyConsumptionsByStayIdQuery(stayId), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetTotalByStayId_returns_total_cost()
    {
        var stayId = Guid.NewGuid();
        var uow = CreateUnitOfWork(out _, out var consumptionRepo);
        consumptionRepo.GetTotalByHospitalizationStayIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(3500m);

        var total = await new GetSupplyConsumptionTotalByStayIdQueryHandler(uow)
            .Handle(new GetSupplyConsumptionTotalByStayIdQuery(stayId), CancellationToken.None);

        Assert.Equal(3500m, total);
    }

    [Fact]
    public async Task Register_throws_NotFoundException_when_stay_does_not_exist()
    {
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 50);
        var uow = CreateUnitOfWork(out var suppliesRepo, out var consumptionRepo, out var staysRepo);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);
        staysRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        var command = new RegisterSupplyConsumptionCommand(Guid.NewGuid(), supply.Id, 5, Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None));

        Assert.Equal(50, supply.Stock);
        await consumptionRepo.DidNotReceive().AddAsync(Arg.Any<SupplyConsumption>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_throws_ConflictException_when_stay_is_discharged_without_touching_stock()
    {
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 50);
        var stay = new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Observación");
        stay.Discharge();

        var uow = CreateUnitOfWork(out var suppliesRepo, out var consumptionRepo, out var staysRepo);
        suppliesRepo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);
        staysRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);

        var command = new RegisterSupplyConsumptionCommand(stay.Id, supply.Id, 5, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => new RegisterSupplyConsumptionCommandHandler(uow)
            .Handle(command, CancellationToken.None));

        Assert.Equal("No se pueden registrar consumos en una estancia dada de alta.", ex.Message);
        Assert.Equal(50, supply.Stock);
        await consumptionRepo.DidNotReceive().AddAsync(Arg.Any<SupplyConsumption>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static IUnitOfWork CreateUnitOfWork(
        out ISupplyRepository suppliesRepo,
        out ISupplyConsumptionRepository consumptionRepo) =>
        CreateUnitOfWork(out suppliesRepo, out consumptionRepo, out _);

    // Por defecto cualquier estancia consultada está activa.
    private static IUnitOfWork CreateUnitOfWork(
        out ISupplyRepository suppliesRepo,
        out ISupplyConsumptionRepository consumptionRepo,
        out IHospitalizationStayRepository staysRepo)
    {
        var uow = Substitute.For<IUnitOfWork>();
        suppliesRepo = Substitute.For<ISupplyRepository>();
        consumptionRepo = Substitute.For<ISupplyConsumptionRepository>();
        staysRepo = Substitute.For<IHospitalizationStayRepository>();
        staysRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HospitalizationStay(Guid.NewGuid(), null, Guid.NewGuid(), "Observación"));
        uow.SuppliesRepository.Returns(suppliesRepo);
        uow.SupplyConsumptionsRepository.Returns(consumptionRepo);
        uow.HospitalizationStaysRepository.Returns(staysRepo);
        return uow;
    }
}
