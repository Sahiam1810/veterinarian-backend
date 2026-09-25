using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.UseCases;
using Application.ProcedureOrders.Abstraction;
using Application.ProcedureOrders.UseCases;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.Veterinarians.Entities;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.MedicalOrders;

public class ProcedureOrderCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IProcedureOrderRepository _orderRepo = Substitute.For<IProcedureOrderRepository>();
    private readonly IVeterinarianRepository _vetRepo = Substitute.For<IVeterinarianRepository>();
    private readonly IAppointmentRepository _appointmentRepo = Substitute.For<IAppointmentRepository>();
    private readonly Application.HospitalizationStays.Abstraction.IHospitalizationStayRepository _stayRepo = Substitute.For<Application.HospitalizationStays.Abstraction.IHospitalizationStayRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CreateProcedureOrderCommandHandler _createHandler;
    private readonly CompleteProcedureOrderCommandHandler _completeHandler;

    private static readonly Guid VetId = Guid.NewGuid();

    public ProcedureOrderCommandHandlerTests()
    {
        _unitOfWork.ProcedureOrdersRepository.Returns(_orderRepo);
        _unitOfWork.VeterinariansRepository.Returns(_vetRepo);
        _unitOfWork.AppointmentsRepository.Returns(_appointmentRepo);
        _unitOfWork.HospitalizationStaysRepository.Returns(_stayRepo);
        _createHandler = new CreateProcedureOrderCommandHandler(_unitOfWork);
        _completeHandler = new CompleteProcedureOrderCommandHandler(_unitOfWork, _sender);
    }


    private Appointment BuildAppointment(Guid appointmentId, Guid? clientPetId = null)
    {
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
    public async Task CreateInHouseProcedureOrder_Succeeds()
    {
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: appointmentId,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput>
            {
                new(Guid.NewGuid(), "Ecografía abdominal completa")
            });


        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsInHouse);
        Assert.Equal("Pendiente", result.Status);
        Assert.Single(result.Items);
        Assert.Equal(VetId, result.VeterinarianId);

        await _orderRepo.Received(1).AddAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReferredProcedureOrder_Succeeds()
    {
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: appointmentId,
            IsInHouse: false,
            ReferredTo: "Centro de Diagnóstico Externo",
            ReferralReason: "Equipo especializado no disponible",
            Items: null);


        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsInHouse);
        Assert.Equal("Pendiente", result.Status);
        Assert.Empty(result.Items);

        await _orderRepo.Received(1).AddAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInHouseProcedureOrder_WithoutItems_ThrowsArgumentException()
    {
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: appointmentId,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateReferredProcedureOrder_WithItems_ThrowsArgumentException()
    {
        var clientPetId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId, clientPetId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: appointmentId,
            IsInHouse: false,
            ReferredTo: "Centro X",
            ReferralReason: "Motivo Y",
            Items: new List<ProcedureOrderItemInput> { new(Guid.NewGuid(), "nota") });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }


    [Fact]
    public async Task CompleteProcedureOrder_SetsStatusAndDispatchesNotification()
    {
        var vetUserId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var veterinarian = new Veterinarian(vetUserId, Guid.NewGuid(), "LIC-12345");
        typeof(Veterinarian).GetProperty("Id")?.SetValue(veterinarian, VetId);

        var order = new ProcedureOrder(
            Guid.NewGuid(),
            VetId,
            appointmentId,
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Prueba X") });

        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        _vetRepo.GetByIdAsync(VetId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        var resultFileUrl = "https://clinica.com/resultados/ecografia_123.pdf";

        await _completeHandler.Handle(
            new CompleteProcedureOrderCommand(order.Id, resultFileUrl),
            CancellationToken.None);

        Assert.Equal("Completada", order.Status);
        Assert.Equal(resultFileUrl, order.ResultFileUrl);

        await _orderRepo.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(cmd =>
                cmd.UserId == vetUserId &&
                cmd.AppointmentId == appointmentId &&
                cmd.Type == "ProcedimientoComp"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteProcedureOrder_FromHospitalizationStay_DoesNotDispatchAppointmentNotification()
    {
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(
            Guid.NewGuid(),
            null,
            VetId,
            "Monitoreo");
        var order = new ProcedureOrder(
            stay.ClientPetId,
            VetId,
            appointmentId: null,
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Rayos X") },
            hospitalizationStayId: stay.Id);

        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await _completeHandler.Handle(
            new CompleteProcedureOrderCommand(order.Id, "resultado.pdf"),
            CancellationToken.None);

        Assert.Equal("Completada", order.Status);
        Assert.Equal("resultado.pdf", order.ResultFileUrl);
        await _orderRepo.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _vetRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteMedicationOrder_DoesNotDispatchNotification()
    {
        // Este test verifica que completar una orden de MEDICAMENTO no dispara notificación
        // (el handler de medication no tiene ISender)
        var medicationOrder = new Domain.MedicationOrders.Entities.MedicationOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Dosis") });

        var medRepo = Substitute.For<Application.MedicationOrders.Abstraction.IMedicationOrderRepository>();
        _unitOfWork.MedicationOrdersRepository.Returns(medRepo);

        medRepo.GetByIdAsync(medicationOrder.Id, Arg.Any<CancellationToken>())
            .Returns(medicationOrder);

        var medCompleteHandler = new Application.MedicationOrders.UseCases.CompleteMedicationOrderCommandHandler(_unitOfWork);
        await medCompleteHandler.Handle(
            new Application.MedicationOrders.UseCases.CompleteMedicationOrderCommand(medicationOrder.Id),
            CancellationToken.None);

        Assert.Equal("Entregada", medicationOrder.Status);
        // ISender NO fue llamado (no hay ISender en el medicationCompleteHandler)
        await _sender.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteProcedureOrder_WhenNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _orderRepo.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((ProcedureOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _completeHandler.Handle(new CompleteProcedureOrderCommand(missingId), CancellationToken.None));
    }

    [Fact]
    public async Task CompleteProcedureOrder_WhenAlreadyCompletada_ThrowsInvalidOperationException()
    {
        var order = new ProcedureOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Prueba") });
        order.Complete(null); // ya completada

        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _completeHandler.Handle(new CompleteProcedureOrderCommand(order.Id), CancellationToken.None));

        await _orderRepo.DidNotReceive().UpdateAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteProcedureOrder_WhenCancelled_ThrowsConflictException()
    {
        var order = new ProcedureOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Prueba") });
        order.Cancel();

        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _completeHandler.Handle(new CompleteProcedureOrderCommand(order.Id), CancellationToken.None));

        await _orderRepo.DidNotReceive().UpdateAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteProcedureOrder_WhenStatusIsNotPending_ThrowsConflictWithoutPersisting()
    {
        var order = new ProcedureOrder(
            Guid.NewGuid(),
            VetId,
            Guid.NewGuid(),
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Prueba") });
        typeof(ProcedureOrder).GetProperty(nameof(ProcedureOrder.Status))!.SetValue(order, "EnProceso");

        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _completeHandler.Handle(new CompleteProcedureOrderCommand(order.Id, "resultado.pdf"), CancellationToken.None));

        Assert.Equal("EnProceso", order.Status);
        await _orderRepo.DidNotReceive().UpdateAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProcedureOrder_WithHospitalizationStay_Succeeds()
    {
        var clientPetId = Guid.NewGuid();
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(clientPetId, null, VetId, "Monitoreo");
        _stayRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var command = new CreateProcedureOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput> { new(Guid.NewGuid(), "Rayos X estancia") },
            HospitalizationStayId: stay.Id);

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(stay.Id, result.HospitalizationStayId);
        Assert.Null(result.AppointmentId);
        Assert.Equal("Pendiente", result.Status);
    }

    [Fact]
    public async Task CreateProcedureOrder_WithDischargedStay_ThrowsConflictException()
    {
        var clientPetId = Guid.NewGuid();
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(clientPetId, null, VetId, "Monitoreo");
        stay.Discharge();
        _stayRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var command = new CreateProcedureOrderCommand(
            ClientPetId: clientPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput> { new(Guid.NewGuid(), "Rayos X alta") },
            HospitalizationStayId: stay.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateProcedureOrder_WithMismatchedPetInStay_ThrowsBadRequestException()
    {
        var stayPetId = Guid.NewGuid();
        var orderPetId = Guid.NewGuid();
        var stay = new Domain.HospitalizationStays.Entities.HospitalizationStay(stayPetId, null, VetId, "Monitoreo");
        _stayRepo.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(stay);

        var command = new CreateProcedureOrderCommand(
            ClientPetId: orderPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput> { new(Guid.NewGuid(), "Rayos X pet mala") },
            HospitalizationStayId: stay.Id);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }


    [Fact]
    public async Task GetPendingProcedureOrders_ReturnsPendingOrdersFromRepository()
    {
        var handler = new GetPendingProcedureOrdersQueryHandler(_unitOfWork);
        var pendingOrders = new List<ProcedureOrder>
        {
            new ProcedureOrder(
                Guid.NewGuid(),
                VetId,
                Guid.NewGuid(),
                isInHouse: true,
                referredTo: null,
                referralReason: null,
                items: new[] { (Guid.NewGuid(), (string?)"Examen X") })
        };

        _orderRepo.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(pendingOrders);

        var result = await handler.Handle(new GetPendingProcedureOrdersQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        await _orderRepo.Received(1).GetPendingAsync(Arg.Any<CancellationToken>());
    }
}
