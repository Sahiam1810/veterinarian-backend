using Application.Common.Abstractions;
using Application.MedicationOrders.Abstraction;
using Application.MedicationOrders.UseCases;
using Domain.MedicationOrders.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.MedicalOrders;

public class MedicationOrderCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMedicationOrderRepository _repository = Substitute.For<IMedicationOrderRepository>();
    private readonly CreateMedicationOrderCommandHandler _createHandler;
    private readonly CompleteMedicationOrderCommandHandler _completeHandler;

    public MedicationOrderCommandHandlerTests()
    {
        _unitOfWork.MedicationOrdersRepository.Returns(_repository);
        _createHandler = new CreateMedicationOrderCommandHandler(_unitOfWork);
        _completeHandler = new CompleteMedicationOrderCommandHandler(_unitOfWork);
    }

    [Fact]
    public async Task CreateInHouseOrder_WithItems_Succeeds()
    {
        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            VeterinarianId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<MedicationOrderItemInput>
            {
                new(Guid.NewGuid(), "1 pastilla cada 8 horas por 5 días")
            });

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsInHouse);
        Assert.Equal("Pendiente", result.Status);
        Assert.Null(result.ReferredTo);
        Assert.Null(result.ReferralReason);
        Assert.Single(result.Items);

        await _repository.Received(1).AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferredOrder_WithReferredDetails_Succeeds()
    {
        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            VeterinarianId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            IsInHouse: false,
            ReferredTo: "Farmacia Veterinaria Central",
            ReferralReason: "Medicamento especial no disponible en clínica",
            Items: null);

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsInHouse);
        Assert.Equal("Pendiente", result.Status);
        Assert.Equal("Farmacia Veterinaria Central", result.ReferredTo);
        Assert.Equal("Medicamento especial no disponible en clínica", result.ReferralReason);
        Assert.Empty(result.Items);

        await _repository.Received(1).AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInHouseOrder_WithoutItems_ThrowsArgumentException()
    {
        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            VeterinarianId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<MedicationOrderItemInput>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateReferredOrder_WithItems_ThrowsArgumentException()
    {
        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            VeterinarianId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            IsInHouse: false,
            ReferredTo: "Centro Externo",
            ReferralReason: "Motivo x",
            Items: new List<MedicationOrderItemInput> { new(Guid.NewGuid(), "Notas") });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteMedicationOrder_ChangesStatusToEntregada()
    {
        var order = new MedicationOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis 1") });

        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await _completeHandler.Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal("Entregada", order.Status);
        await _repository.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
