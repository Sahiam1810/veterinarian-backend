using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.MedicationOrders.Abstraction;
using Application.MedicationOrders.UseCases;
using Domain.Appointments.Entities;
using Domain.MedicationOrders.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.MedicalOrders;

public class MedicationOrderCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMedicationOrderRepository _repository = Substitute.For<IMedicationOrderRepository>();
    private readonly IAppointmentRepository _appointmentRepo = Substitute.For<IAppointmentRepository>();
    private readonly CreateMedicationOrderCommandHandler _createHandler;
    private readonly CompleteMedicationOrderCommandHandler _completeHandler;

    private static readonly Guid VetId = Guid.NewGuid();

    public MedicationOrderCommandHandlerTests()
    {
        _unitOfWork.MedicationOrdersRepository.Returns(_repository);
        _unitOfWork.AppointmentsRepository.Returns(_appointmentRepo);
        _createHandler = new CreateMedicationOrderCommandHandler(_unitOfWork);
        _completeHandler = new CompleteMedicationOrderCommandHandler(_unitOfWork);
    }

    private Appointment BuildAppointment(Guid appointmentId)
    {
        // Construir una cita mínima con VeterinarianId
        var apt = (Appointment)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Appointment));
        typeof(Appointment).GetProperty("Id")?.SetValue(apt, appointmentId);
        typeof(Appointment).GetProperty("VeterinarianId")?.SetValue(apt, VetId);
        return apt;
    }

    [Fact]
    public async Task CreateInHouseOrder_WithItems_Succeeds()
    {
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            AppointmentId: appointmentId,
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
        Assert.Equal(VetId, result.VeterinarianId);

        await _repository.Received(1).AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferredOrder_WithReferredDetails_Succeeds()
    {
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            AppointmentId: appointmentId,
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
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            AppointmentId: appointmentId,
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
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            AppointmentId: appointmentId,
            IsInHouse: false,
            ReferredTo: "Centro Externo",
            ReferralReason: "Motivo x",
            Items: new List<MedicationOrderItemInput> { new(Guid.NewGuid(), "Notas") });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMedicationOrder_WhenAppointmentNotFound_ThrowsNotFoundException()
    {
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new CreateMedicationOrderCommand(
            ClientPetId: Guid.NewGuid(),
            AppointmentId: appointmentId,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<MedicationOrderItemInput> { new(Guid.NewGuid(), null) });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteMedicationOrder_ChangesStatusToEntregada()
    {
        var order = new MedicationOrder(
            Guid.NewGuid(),
            VetId,
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

    [Fact]
    public async Task CompleteMedicationOrder_WhenNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _repository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((MedicationOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _completeHandler.Handle(new CompleteMedicationOrderCommand(missingId), CancellationToken.None));
    }

    [Fact]
    public async Task CompleteMedicationOrder_WhenAlreadyEntregada_ThrowsInvalidOperationException()
    {
        var order = new MedicationOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis") });
        order.Complete(); // ya está entregada

        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _completeHandler.Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None));
    }
}
