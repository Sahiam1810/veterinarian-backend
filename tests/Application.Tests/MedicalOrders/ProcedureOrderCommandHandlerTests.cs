using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.HospitalizationStays.Abstraction;
using Application.Notifications.UseCases;
using Application.Procedures.Abstraction;
using Application.ProcedureOrders.Abstraction;
using Application.ProcedureOrders.UseCases;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.HospitalizationStays.Entities;
using Domain.Procedures.Entities;
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
    private readonly IHospitalizationStayRepository _stayRepo = Substitute.For<IHospitalizationStayRepository>();
    private readonly IProcedureRepository _procedureRepo = Substitute.For<IProcedureRepository>();
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
        _unitOfWork.ProceduresRepository.Returns(_procedureRepo);
        _createHandler = new CreateProcedureOrderCommandHandler(_unitOfWork);
        _completeHandler = new CompleteProcedureOrderCommandHandler(_unitOfWork, _sender);
    }

    private Appointment BuildAppointment(Guid appointmentId)
    {
        var apt = (Appointment)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Appointment));
        typeof(Appointment).GetProperty("Id")?.SetValue(apt, appointmentId);
        typeof(Appointment).GetProperty("VeterinarianId")?.SetValue(apt, VetId);
        return apt;
    }

    [Fact]
    public async Task CreateInHouseProcedureOrder_Succeeds()
    {
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: Guid.NewGuid(),
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
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: Guid.NewGuid(),
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
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: Guid.NewGuid(),
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
        var appointmentId = Guid.NewGuid();
        _appointmentRepo.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns(BuildAppointment(appointmentId));

        var command = new CreateProcedureOrderCommand(
            ClientPetId: Guid.NewGuid(),
            AppointmentId: appointmentId,
            IsInHouse: false,
            ReferredTo: "Centro X",
            ReferralReason: "Motivo Y",
            Items: new List<ProcedureOrderItemInput> { new(Guid.NewGuid(), "nota") });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateProcedureOrder_ForActiveStay_CopiesCatalogPrice()
    {
        var petId = Guid.NewGuid();
        var procedureId = Guid.NewGuid();
        var stayId = Guid.NewGuid();
        var procedure = new Procedure("Radiografía", "RX", true, 420m);
        typeof(Procedure).GetProperty(nameof(Procedure.Id))!.SetValue(procedure, procedureId);

        var stay = new HospitalizationStay(petId, null, Guid.NewGuid(), "Observación");
        typeof(HospitalizationStay).GetProperty(nameof(HospitalizationStay.Id))!.SetValue(stay, stayId);

        _stayRepo.GetByIdAsync(stayId, Arg.Any<CancellationToken>()).Returns(stay);
        _procedureRepo.GetByIdAsync(procedureId, Arg.Any<CancellationToken>()).Returns(procedure);

        var command = new CreateProcedureOrderCommand(
            ClientPetId: petId,
            AppointmentId: null,
            HospitalizationStayId: stayId,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput> { new(procedureId, "Rx torácica") });

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.Equal(stayId, result.HospitalizationStayId);
        Assert.Null(result.AppointmentId);
        Assert.Single(result.Items);
        Assert.Equal(420m, result.Items.First().UnitPrice);
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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _completeHandler.Handle(new CompleteProcedureOrderCommand(order.Id), CancellationToken.None));
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
