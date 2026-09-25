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
    private readonly Application.HospitalizationStays.Abstraction.IHospitalizationStayRepository _stayRepo = Substitute.For<Application.HospitalizationStays.Abstraction.IHospitalizationStayRepository>();
    private readonly CreateMedicationOrderCommandHandler _createHandler;
    private readonly CompleteMedicationOrderCommandHandler _completeHandler;

    private static readonly Guid VetId = Guid.NewGuid();

    public MedicationOrderCommandHandlerTests()
    {
        _unitOfWork.MedicationOrdersRepository.Returns(_repository);
        _unitOfWork.AppointmentsRepository.Returns(_appointmentRepo);
        _unitOfWork.HospitalizationStaysRepository.Returns(_stayRepo);
        _createHandler = new CreateMedicationOrderCommandHandler(_unitOfWork);
        _completeHandler = new CompleteMedicationOrderCommandHandler(_unitOfWork);
    }


    private Appointment BuildAppointment(Guid appointmentId, Guid? clientPetId = null)
    {
        // Construir una cita mínima con VeterinarianId
        var apt = (Appointment)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Appointment));
        typeof(Appointment).GetProperty("Id")?.SetValue(apt, appointmentId);
        typeof(Appointment).GetProperty("VeterinarianId")?.SetValue(apt, VetId);
        if (clientPetId.HasValue)
        {
            typeof(Appointment).GetProperty("ClientPetId")?.SetValue(apt, clientPetId.Value);
        }
        return apt;
    }


    [Fact]
    public async Task CreateInHouseOrder_WithItems_Succeeds()
    {
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: clientPetId,
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
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: clientPetId,
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
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: clientPetId,
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
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateMedicationOrderCommand(
            ClientPetId: clientPetId,
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

        await Assert.ThrowsAsync<ConflictException>(() =>
            _completeHandler.Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteMedicationOrder_WhenCancelled_ThrowsConflictException()
    {
        var order = new MedicationOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis") });
        order.Cancel();

        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _completeHandler.Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteMedicationOrder_WhenStatusIsNotPending_ThrowsConflictWithoutPersisting()
    {
        var order = new MedicationOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis") });
        typeof(MedicationOrder).GetProperty(nameof(MedicationOrder.Status))!.SetValue(order, "EnPreparacion");

        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _completeHandler.Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None));

        Assert.Equal("EnPreparacion", order.Status);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMedicationOrder_WithHospitalizationStay_Succeeds()
    {
        var clientPetId = Guid.NewGuid();
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(clientPetId, null, VetId, "Chequeo diario");
        _stayRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var command = new CreateMedicationOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<MedicationOrderItemInput> { new(Guid.NewGuid(), "Dosis estancia") },
            HospitalizationStayId: stay.Id);

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(stay.Id, result.HospitalizationStayId);
        Assert.Null(result.AppointmentId);
        Assert.Equal("Pendiente", result.Status);
    }

    [Fact]
    public async Task CreateMedicationOrder_WithDischargedStay_ThrowsConflictException()
    {
        var clientPetId = Guid.NewGuid();
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(clientPetId, null, VetId, "Chequeo");
        stay.Discharge();
        _stayRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var command = new CreateMedicationOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<MedicationOrderItemInput> { new(Guid.NewGuid(), "Dosis alta") },
            HospitalizationStayId: stay.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMedicationOrder_WithMismatchedPetInStay_ThrowsBadRequestException()
    {
        var stayPetId = Guid.NewGuid();
        var orderPetId = Guid.NewGuid();
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(stayPetId, null, VetId, "Chequeo");
        _stayRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var command = new CreateMedicationOrderCommand(
            ClientPetId: orderPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<MedicationOrderItemInput> { new(Guid.NewGuid(), "Dosis mala") },
            HospitalizationStayId: stay.Id);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }


    [Fact]
    public async Task GetPendingMedicationOrders_ReturnsPendingOrdersFromRepository()
    {
        var handler = new GetPendingMedicationOrdersQueryHandler(_unitOfWork);
        var pendingOrders = new List<MedicationOrder>
        {
            new MedicationOrder(
                Guid.NewGuid(),
                VetId,
                Guid.NewGuid(),
                isInHouse: true,
                referredTo: null,
                referralReason: null,
                items: new[] { (Guid.NewGuid(), (string?)"Dosis 1") })
        };

        _repository.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(pendingOrders);

        var result = await handler.Handle(new GetPendingMedicationOrdersQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        await _repository.Received(1).GetPendingAsync(Arg.Any<CancellationToken>());
    }
}
