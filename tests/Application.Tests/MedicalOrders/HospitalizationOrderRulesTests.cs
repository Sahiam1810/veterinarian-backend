using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.HospitalizationStays.Abstraction;
using Application.MedicationOrders.Abstraction;
using Application.MedicationOrders.UseCases;
using Application.ProcedureOrders.Abstraction;
using Application.ProcedureOrders.UseCases;
using Application.Veterinarians.Abstraction;
using Domain.Appointments.Entities;
using Domain.HospitalizationStays.Entities;
using Domain.MedicalOrders;
using Domain.MedicationOrders.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.Veterinarians.Entities;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.MedicalOrders;

// Órdenes con origen en una estancia de hospitalización y transiciones de estado.
public sealed class HospitalizationOrderRulesTests
{
    private static readonly Guid ClientPetId = Guid.NewGuid();
    private static readonly Guid VetUserId = Guid.NewGuid();

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMedicationOrderRepository medicationOrders = Substitute.For<IMedicationOrderRepository>();
    private readonly IProcedureOrderRepository procedureOrders = Substitute.For<IProcedureOrderRepository>();
    private readonly IHospitalizationStayRepository stays = Substitute.For<IHospitalizationStayRepository>();
    private readonly IVeterinarianRepository veterinarians = Substitute.For<IVeterinarianRepository>();
    private readonly IAppointmentRepository appointments = Substitute.For<IAppointmentRepository>();
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly Veterinarian veterinarian = new(VetUserId, Guid.NewGuid(), "LIC-001");

    public HospitalizationOrderRulesTests()
    {
        unitOfWork.MedicationOrdersRepository.Returns(medicationOrders);
        unitOfWork.ProcedureOrdersRepository.Returns(procedureOrders);
        unitOfWork.HospitalizationStaysRepository.Returns(stays);
        unitOfWork.VeterinariansRepository.Returns(veterinarians);
        unitOfWork.AppointmentsRepository.Returns(appointments);
        veterinarians.GetByUserIdAsync(VetUserId, Arg.Any<CancellationToken>()).Returns(veterinarian);
    }

    private HospitalizationStay ArrangeStay(bool discharged = false, Guid? petId = null)
    {
        var stay = new HospitalizationStay(petId ?? ClientPetId, null, Guid.NewGuid(), "Observación");
        if (discharged)
        {
            stay.Discharge();
        }

        stays.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);
        return stay;
    }

    private static CreateMedicationOrderCommand MedicationCommand(
        Guid? appointmentId,
        Guid? stayId,
        Guid actorUserId) =>
        new(
            ClientPetId,
            appointmentId,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: [new MedicationOrderItemInput(Guid.NewGuid(), "Cada 8 horas")],
            HospitalizationStayId: stayId,
            ActorUserId: actorUserId);

    private static CreateProcedureOrderCommand ProcedureCommand(Guid? stayId, Guid actorUserId) =>
        new(
            ClientPetId,
            AppointmentId: null,
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: [new ProcedureOrderItemInput(Guid.NewGuid(), "Hemograma")],
            HospitalizationStayId: stayId,
            ActorUserId: actorUserId);

    // ---------- Crear órdenes en una estancia ----------

    [Fact]
    public async Task Create_medication_order_in_active_stay_is_signed_by_the_prescribing_veterinarian()
    {
        var stay = ArrangeStay();

        var order = await new CreateMedicationOrderCommandHandler(unitOfWork)
            .Handle(MedicationCommand(null, stay.Id, VetUserId), CancellationToken.None);

        Assert.Equal(stay.Id, order.HospitalizationStayId);
        Assert.Null(order.AppointmentId);
        Assert.Equal(veterinarian.Id, order.VeterinarianId);
        await medicationOrders.Received(1).AddAsync(order, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_procedure_order_in_active_stay_is_signed_by_the_prescribing_veterinarian()
    {
        var stay = ArrangeStay();

        var order = await new CreateProcedureOrderCommandHandler(unitOfWork)
            .Handle(ProcedureCommand(stay.Id, VetUserId), CancellationToken.None);

        Assert.Equal(stay.Id, order.HospitalizationStayId);
        Assert.Null(order.AppointmentId);
        Assert.Equal(veterinarian.Id, order.VeterinarianId);
        await procedureOrders.Received(1).AddAsync(order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_medication_order_in_discharged_stay_returns_conflict_without_persisting()
    {
        var stay = ArrangeStay(discharged: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            new CreateMedicationOrderCommandHandler(unitOfWork)
                .Handle(MedicationCommand(null, stay.Id, VetUserId), CancellationToken.None));

        Assert.Equal("No se pueden crear órdenes en una estancia dada de alta.", ex.Message);
        await medicationOrders.DidNotReceive().AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_procedure_order_in_discharged_stay_returns_conflict_without_persisting()
    {
        var stay = ArrangeStay(discharged: true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new CreateProcedureOrderCommandHandler(unitOfWork)
                .Handle(ProcedureCommand(stay.Id, VetUserId), CancellationToken.None));

        await procedureOrders.DidNotReceive().AddAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_order_for_a_pet_different_from_the_stay_returns_bad_request()
    {
        var stay = ArrangeStay(petId: Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            new CreateMedicationOrderCommandHandler(unitOfWork)
                .Handle(MedicationCommand(null, stay.Id, VetUserId), CancellationToken.None));

        Assert.Equal("La mascota de la orden no coincide con la de la estancia.", ex.Message);
        await medicationOrders.DidNotReceive().AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_order_for_missing_stay_returns_not_found()
    {
        stays.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HospitalizationStay?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new CreateMedicationOrderCommandHandler(unitOfWork)
                .Handle(MedicationCommand(null, Guid.NewGuid(), VetUserId), CancellationToken.None));

        await medicationOrders.DidNotReceive().AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_order_in_stay_by_a_non_veterinarian_returns_forbidden()
    {
        var stay = ArrangeStay();
        var adminUserId = Guid.NewGuid();
        veterinarians.GetByUserIdAsync(adminUserId, Arg.Any<CancellationToken>()).Returns((Veterinarian?)null);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            new CreateProcedureOrderCommandHandler(unitOfWork)
                .Handle(ProcedureCommand(stay.Id, adminUserId), CancellationToken.None));

        Assert.Equal("Solo un veterinario puede crear órdenes de hospitalización.", ex.Message);
        await procedureOrders.DidNotReceive().AddAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_order_with_both_origins_returns_bad_request()
    {
        var stay = ArrangeStay();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            new CreateMedicationOrderCommandHandler(unitOfWork)
                .Handle(MedicationCommand(Guid.NewGuid(), stay.Id, VetUserId), CancellationToken.None));

        await medicationOrders.DidNotReceive().AddAsync(Arg.Any<MedicationOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_order_from_appointment_keeps_the_appointment_veterinarian()
    {
        var appointmentId = Guid.NewGuid();
        var appointmentVetId = Guid.NewGuid();
        var appointment = (Appointment)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Appointment));
        typeof(Appointment).GetProperty("Id")?.SetValue(appointment, appointmentId);
        typeof(Appointment).GetProperty("VeterinarianId")?.SetValue(appointment, appointmentVetId);
        appointments.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>()).Returns(appointment);

        // Aunque quien la crea sea otro veterinario, la orden de cita la firma el de la cita.
        var order = await new CreateMedicationOrderCommandHandler(unitOfWork)
            .Handle(MedicationCommand(appointmentId, null, VetUserId), CancellationToken.None);

        Assert.Equal(appointmentId, order.AppointmentId);
        Assert.Null(order.HospitalizationStayId);
        Assert.Equal(appointmentVetId, order.VeterinarianId);
        await veterinarians.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void Validator_requires_exactly_one_origin(bool withAppointment, bool withStay, bool expectedValid)
    {
        var command = MedicationCommand(
            withAppointment ? Guid.NewGuid() : null,
            withStay ? Guid.NewGuid() : null,
            VetUserId);

        var result = new CreateMedicationOrderCommandValidator().Validate(command);

        Assert.Equal(expectedValid, result.IsValid);
    }

    // ---------- Transiciones de estado ----------

    private static MedicationOrder PendingMedicationOrder() =>
        new(ClientPetId, Guid.NewGuid(), null, true, null, null,
            [(Guid.NewGuid(), (string?)"Dosis")], hospitalizationStayId: Guid.NewGuid());

    private static ProcedureOrder PendingProcedureOrder() =>
        new(ClientPetId, Guid.NewGuid(), null, true, null, null,
            [(Guid.NewGuid(), (string?)"Prueba")], hospitalizationStayId: Guid.NewGuid());

    [Fact]
    public async Task Deliver_pending_medication_order_succeeds()
    {
        var order = PendingMedicationOrder();
        medicationOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await new CompleteMedicationOrderCommandHandler(unitOfWork)
            .Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(MedicationOrder.DeliveredStatus, order.Status);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deliver_cancelled_medication_order_returns_conflict_without_persisting()
    {
        var order = PendingMedicationOrder();
        order.Cancel();
        medicationOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            new CompleteMedicationOrderCommandHandler(unitOfWork)
                .Handle(new CompleteMedicationOrderCommand(order.Id), CancellationToken.None));

        Assert.Equal("La orden de medicamento está cancelada y no se puede modificar.", ex.Message);
        Assert.Equal(MedicationOrder.CancelledStatus, order.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_pending_procedure_order_from_stay_saves_result_without_appointment_notification()
    {
        var order = PendingProcedureOrder();
        procedureOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await new CompleteProcedureOrderCommandHandler(unitOfWork, sender)
            .Handle(new CompleteProcedureOrderCommand(order.Id, "https://resultado"), CancellationToken.None);

        Assert.Equal(ProcedureOrder.CompletedStatus, order.Status);
        Assert.Equal("https://resultado", order.ResultFileUrl);
        await sender.DidNotReceiveWithAnyArgs().Send(default(IRequest<Guid>)!, default);
    }

    [Fact]
    public async Task Complete_cancelled_procedure_order_returns_conflict_without_persisting()
    {
        var order = PendingProcedureOrder();
        order.Cancel();
        procedureOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new CompleteProcedureOrderCommandHandler(unitOfWork, sender)
                .Handle(new CompleteProcedureOrderCommand(order.Id, "https://resultado"), CancellationToken.None));

        Assert.Null(order.ResultFileUrl);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Consulta por estancia ----------

    [Fact]
    public async Task Get_orders_by_stay_includes_the_origin_appointment()
    {
        var appointmentId = Guid.NewGuid();
        var stay = new HospitalizationStay(ClientPetId, appointmentId, Guid.NewGuid(), "Observación");
        stays.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);
        medicationOrders.GetByHospitalizationStayAsync(stay.Id, appointmentId, Arg.Any<CancellationToken>())
            .Returns([PendingMedicationOrder()]);

        var result = await new GetMedicationOrdersByHospitalizationStayIdQueryHandler(unitOfWork)
            .Handle(new GetMedicationOrdersByHospitalizationStayIdQuery(stay.Id), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Get_orders_by_missing_stay_returns_not_found()
    {
        stays.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HospitalizationStay?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetProcedureOrdersByHospitalizationStayIdQueryHandler(unitOfWork)
                .Handle(new GetProcedureOrdersByHospitalizationStayIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    // ---------- Reglas de dominio ----------

    [Fact]
    public void Domain_rejects_order_without_origin_or_with_both()
    {
        Assert.Throws<ArgumentException>(() =>
            new MedicationOrder(ClientPetId, Guid.NewGuid(), null, true, null, null, [(Guid.NewGuid(), (string?)null)]));
        Assert.Throws<ArgumentException>(() =>
            new ProcedureOrder(ClientPetId, Guid.NewGuid(), Guid.NewGuid(), true, null, null,
                [(Guid.NewGuid(), (string?)null)], hospitalizationStayId: Guid.NewGuid()));
    }

    [Fact]
    public void Domain_treats_empty_guid_as_missing_origin()
    {
        var (appointmentId, stayId) = MedicalOrderOriginRules.Normalize(Guid.Empty, Guid.NewGuid());

        Assert.Null(appointmentId);
        Assert.NotNull(stayId);
    }

    [Fact]
    public void Domain_does_not_allow_transitions_after_delivery()
    {
        var order = PendingMedicationOrder();
        order.Complete();

        Assert.Throws<InvalidOperationException>(() => order.Complete());
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Equal(MedicationOrder.DeliveredStatus, order.Status);
    }
}
