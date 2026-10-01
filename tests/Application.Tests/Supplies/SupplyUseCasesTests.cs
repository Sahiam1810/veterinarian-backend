using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Supplies.Abstraction;
using Application.Supplies.UseCases;
using Domain.Supplies.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Supplies;

public sealed class SupplyUseCasesTests
{
    [Fact]
    public async Task GetAll_returns_supplies_from_repository()
    {
        var uow = CreateUnitOfWork(out var repo);
        var supplies = new List<Supply>
        {
            new("Gasa esteril", "unidad", 1500m, 100),
            new("Jeringa 5ml", "unidad", 800m, 50)
        };
        repo.GetAllAsync(true, Arg.Any<CancellationToken>()).Returns(supplies);

        var result = await new GetAllSuppliesQueryHandler(uow)
            .Handle(new GetAllSuppliesQuery(OnlyActive: true), CancellationToken.None);

        Assert.Equal(2, result.Count());
        await repo.Received(1).GetAllAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_returns_supply_when_found()
    {
        var uow = CreateUnitOfWork(out var repo);
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 100);
        repo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        var result = await new GetSupplyByIdQueryHandler(uow)
            .Handle(new GetSupplyByIdQuery(supply.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Gasa esteril", result.Name);
    }

    [Fact]
    public async Task GetById_throws_NotFoundException_when_not_found()
    {
        var uow = CreateUnitOfWork(out var repo);
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supply?)null);

        var action = () => new GetSupplyByIdQueryHandler(uow)
            .Handle(new GetSupplyByIdQuery(id), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task Create_adds_supply_and_saves_changes()
    {
        var uow = CreateUnitOfWork(out var repo);
        var command = new CreateSupplyCommand("Suero Fisiologico", "bolsa 500ml", 12000m, 30);

        var result = await new CreateSupplyCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Suero Fisiologico", result.Name);
        Assert.Equal("bolsa 500ml", result.Unit);
        Assert.Equal(12000m, result.UnitPrice);
        Assert.Equal(30, result.Stock);
        await repo.Received(1).AddAsync(Arg.Is<Supply>(s => s.Name == "Suero Fisiologico"), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_updates_supply_when_found()
    {
        var uow = CreateUnitOfWork(out var repo);
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 100);
        repo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        var command = new UpdateSupplyCommand(supply.Id, "Gasa esteril 10x10", "caja", 5000m, 20, true);

        await new UpdateSupplyCommandHandler(uow)
            .Handle(command, CancellationToken.None);

        Assert.Equal("Gasa esteril 10x10", supply.Name);
        Assert.Equal("caja", supply.Unit);
        Assert.Equal(5000m, supply.UnitPrice);
        Assert.Equal(20, supply.Stock);
        await repo.Received(1).UpdateAsync(supply, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_deactivates_supply_when_found()
    {
        var uow = CreateUnitOfWork(out var repo);
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 100);
        repo.GetByIdAsync(supply.Id, Arg.Any<CancellationToken>()).Returns(supply);

        await new DeleteSupplyCommandHandler(uow)
            .Handle(new DeleteSupplyCommand(supply.Id), CancellationToken.None);

        await repo.Received(1).DeleteAsync(supply.Id, Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DeductStock_reduces_stock_when_sufficient()
    {
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 10);
        supply.DeductStock(3);
        Assert.Equal(7, supply.Stock);
    }

    [Fact]
    public void DeductStock_throws_when_stock_insufficient()
    {
        var supply = new Supply("Gasa esteril", "unidad", 1500m, 2);
        var ex = Assert.Throws<InvalidOperationException>(() => supply.DeductStock(5));
        Assert.Equal("Stock insuficiente.", ex.Message);
    }

    private static IUnitOfWork CreateUnitOfWork(out ISupplyRepository repository)
    {
        var uow = Substitute.For<IUnitOfWork>();
        repository = Substitute.For<ISupplyRepository>();
        uow.SuppliesRepository.Returns(repository);
        return uow;
    }
}
