using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.HospitalizationStays.Abstraction;
using Application.HospitalizationStays.Dtos;
using Application.HospitalizationStays.UseCases;
using Application.MedicationOrders.Abstraction;
using Application.ProcedureOrders.Abstraction;
using Application.Supplies.Abstraction;
using Application.SupplyConsumptions.Abstraction;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.HospitalizationStays.Entities;
using Domain.MedicationOrders.Entities;
using Domain.Medications.Entities;
using Domain.Pets.Entities;
using Domain.ProcedureOrders.Entities;
using Domain.Procedures.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.Supplies.Entities;
using Domain.SupplyConsumptions.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Hospitalization;

public sealed class HospitalizationStayInvoiceQueryHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IHospitalizationStayRepository staysRepository = Substitute.For<IHospitalizationStayRepository>();
    private readonly ISupplyConsumptionRepository supplyConsumptionsRepository = Substitute.For<ISupplyConsumptionRepository>();
    private readonly ISupplyRepository suppliesRepository = Substitute.For<ISupplyRepository>();
    private readonly IMedicationOrderRepository medicationOrdersRepository = Substitute.For<IMedicationOrderRepository>();
    private readonly IProcedureOrderRepository procedureOrdersRepository = Substitute.For<IProcedureOrderRepository>();

    private readonly GetHospitalizationStayInvoiceQueryHandler handler;

    private static readonly Guid ClientPetId = Guid.NewGuid();
    private static readonly Guid AdmittedByUserId = Guid.NewGuid();
    private static readonly Guid VeterinarianId = Guid.NewGuid();
    private static readonly Guid AppointmentId = Guid.NewGuid();

    private static readonly SpeciesEntity DefaultSpecies = new("Perro");
    private static readonly RaceEntity DefaultRace = new("Labrador", DefaultSpecies);
    private static readonly ClientEntity DefaultClient = new("Juan Pérez", "juan@vet.com", "12345678", "5551010", null);
    private static readonly PetEntity DefaultPet = new("Luna", 4, "F", 10.0m, null, DefaultSpecies, DefaultRace);
    private static readonly ClientPetEntity DefaultClientPet = CreateClientPet();

    private static ClientPetEntity CreateClientPet()
    {
        var cp = new ClientPetEntity(DefaultClient, DefaultPet, true);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Client))!.SetValue(cp, DefaultClient);
        typeof(ClientPetEntity).GetProperty(nameof(ClientPetEntity.Pet))!.SetValue(cp, DefaultPet);
        return cp;
    }

    public HospitalizationStayInvoiceQueryHandlerTests()
    {
        unitOfWork.HospitalizationStaysRepository.Returns(staysRepository);
        unitOfWork.SupplyConsumptionsRepository.Returns(supplyConsumptionsRepository);
        unitOfWork.SuppliesRepository.Returns(suppliesRepository);
        unitOfWork.MedicationOrdersRepository.Returns(medicationOrdersRepository);
        unitOfWork.ProcedureOrdersRepository.Returns(procedureOrdersRepository);

        handler = new GetHospitalizationStayInvoiceQueryHandler(unitOfWork);
    }

    private HospitalizationStay CreateStay(bool discharged = false, bool isPaid = false)
    {
        var stay = new HospitalizationStay(ClientPetId, AppointmentId, AdmittedByUserId, "Observación médica");

        // Set ClientPet entity for navigation properties
        typeof(HospitalizationStay).GetProperty(nameof(HospitalizationStay.ClientPet))!
            .SetValue(stay, DefaultClientPet);

        // Adjust admission date to 4 days ago
        typeof(HospitalizationStay).GetProperty(nameof(HospitalizationStay.FechaIngreso))!
            .SetValue(stay, DateTime.UtcNow.AddDays(-4));

        if (discharged)
        {
            stay.Discharge();
        }

        if (isPaid)
        {
            stay.RegisterPayment();
        }

        return stay;
    }

    [Fact]
    public async Task INVOICE_T01_returns_404_when_stay_not_found()
    {
        var stayId = Guid.NewGuid();
        staysRepository.GetByIdAsync(stayId, Arg.Any<CancellationToken>())
            .Returns((HospitalizationStay?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetHospitalizationStayInvoiceQuery(stayId), CancellationToken.None));
    }

    [Fact]
    public async Task INVOICE_T02_returns_invoice_for_active_stay_with_correct_calculations()
    {
        var stay = CreateStay(discharged: false, isPaid: false);
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);

        supplyConsumptionsRepository.GetByHospitalizationStayIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<SupplyConsumption>());

        medicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<MedicationOrder>());

        procedureOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProcedureOrder>());

        var result = await handler.Handle(new GetHospitalizationStayInvoiceQuery(stay.Id), CancellationToken.None);

        Assert.Equal(stay.Id, result.StayId);
        Assert.Equal("Luna", result.PetName);
        Assert.Equal("Juan Pérez", result.OwnerName);
        Assert.Equal("Activa", result.Status);
        Assert.Equal(50000m, result.DailyRate);
        Assert.Equal(4, result.BilledDays);
        Assert.Equal(200000m, result.HospitalizationTotal);
        Assert.Equal(0m, result.SuppliesTotal);
        Assert.Equal(0m, result.MedicationsTotal);
        Assert.Equal(0m, result.ProceduresTotal);
        Assert.Equal(200000m, result.Total);
        Assert.False(result.IsPaid);
        Assert.Null(result.PaidAt);
    }

    [Fact]
    public async Task INVOICE_T03_returns_invoice_for_discharged_and_paid_stay_preserving_payment_info()
    {
        var stay = CreateStay(discharged: true, isPaid: true);
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);

        supplyConsumptionsRepository.GetByHospitalizationStayIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<SupplyConsumption>());

        medicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<MedicationOrder>());

        procedureOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProcedureOrder>());

        var result = await handler.Handle(new GetHospitalizationStayInvoiceQuery(stay.Id), CancellationToken.None);

        Assert.Equal("Dada de alta", result.Status);
        Assert.True(result.IsPaid);
        Assert.NotNull(result.PaidAt);
        Assert.NotNull(result.DischargedAt);
    }

    [Fact]
    public async Task INVOICE_T04_includes_supplies_and_calculates_supplies_total()
    {
        var stay = CreateStay(discharged: true);
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);

        var supplyId = Guid.NewGuid();
        var supply = new Supply("Suero fisiológico", "Bolsa 500ml", 15000m, 50m);
        typeof(Supply).GetProperty(nameof(Supply.Id))!.SetValue(supply, supplyId);

        var consumption = new SupplyConsumption(stay.Id, supplyId, 2m, 15000m, AdmittedByUserId, "Uso durante curación");

        supplyConsumptionsRepository.GetByHospitalizationStayIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { consumption });

        suppliesRepository.GetByIdAsync(supplyId, Arg.Any<CancellationToken>())
            .Returns(supply);

        medicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<MedicationOrder>());

        procedureOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProcedureOrder>());

        var result = await handler.Handle(new GetHospitalizationStayInvoiceQuery(stay.Id), CancellationToken.None);

        Assert.Single(result.Supplies);
        var item = result.Supplies[0];
        Assert.Equal("Suero fisiológico", item.Name);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(15000m, item.UnitPrice);
        Assert.Equal(30000m, item.Total);
        Assert.Equal("Uso durante curación", item.Notes);
        Assert.Equal(30000m, result.SuppliesTotal);
        Assert.Equal(230000m, result.Total);
    }

    [Fact]
    public async Task INVOICE_T05_includes_delivered_medications_and_excludes_pending_medications()
    {
        var stay = CreateStay(discharged: false);
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);

        supplyConsumptionsRepository.GetByHospitalizationStayIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<SupplyConsumption>());

        var med1 = new Medication("Amoxicilina 500mg");
        var med2 = new Medication("Analgesico");

        var deliveredOrder = new MedicationOrder(stay.ClientPetId, VeterinarianId, stay.AppointmentId!.Value, true, null, null,
            new (Guid MedicationId, string? Notes)[] { (med1.Id, "1 cada 8 horas") });
        deliveredOrder.Complete(); // Status: Entregada

        var itemProp = typeof(MedicationOrderItem).GetProperty(nameof(MedicationOrderItem.Medication))!;
        itemProp.SetValue(deliveredOrder.Items.First(), med1);

        var pendingOrder = new MedicationOrder(stay.ClientPetId, VeterinarianId, stay.AppointmentId!.Value, true, null, null,
            new (Guid MedicationId, string? Notes)[] { (med2.Id, "Si presenta dolor") }); // Status: Pendiente

        itemProp.SetValue(pendingOrder.Items.First(), med2);

        medicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(new[] { deliveredOrder, pendingOrder });

        procedureOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProcedureOrder>());

        var result = await handler.Handle(new GetHospitalizationStayInvoiceQuery(stay.Id), CancellationToken.None);

        Assert.Single(result.Medications);
        Assert.Equal("Amoxicilina 500mg", result.Medications[0].Name);
        Assert.Equal("1 cada 8 horas", result.Medications[0].Notes);
    }

    [Fact]
    public async Task INVOICE_T06_includes_completed_procedures_and_excludes_pending_procedures()
    {
        var stay = CreateStay(discharged: false);
        staysRepository.GetByIdAsync(stay.Id, Arg.Any<CancellationToken>()).Returns(stay);

        supplyConsumptionsRepository.GetByHospitalizationStayIdAsync(stay.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<SupplyConsumption>());

        medicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<MedicationOrder>());

        var proc1 = new Procedure("Hemograma completo");
        var proc2 = new Procedure("Ecografía abdominal");

        var completedOrder = new ProcedureOrder(stay.ClientPetId, VeterinarianId, stay.AppointmentId!.Value, true, null, null,
            new (Guid ProcedureId, string? Notes)[] { (proc1.Id, "Urgente") });
        completedOrder.Complete(); // Status: Completada

        var itemProp = typeof(ProcedureOrderItem).GetProperty(nameof(ProcedureOrderItem.Procedure))!;
        itemProp.SetValue(completedOrder.Items.First(), proc1);

        var pendingOrder = new ProcedureOrder(stay.ClientPetId, VeterinarianId, stay.AppointmentId!.Value, true, null, null,
            new (Guid ProcedureId, string? Notes)[] { (proc2.Id, "Rutina") }); // Status: Pendiente

        itemProp.SetValue(pendingOrder.Items.First(), proc2);

        procedureOrdersRepository.GetByHospitalizationStayAsync(
            stay.AppointmentId, stay.ClientPetId, stay.FechaIngreso, stay.FechaAlta, Arg.Any<CancellationToken>())
            .Returns(new[] { completedOrder, pendingOrder });

        var result = await handler.Handle(new GetHospitalizationStayInvoiceQuery(stay.Id), CancellationToken.None);

        Assert.Single(result.Procedures);
        Assert.Equal("Hemograma completo", result.Procedures[0].Name);
        Assert.Equal("Urgente", result.Procedures[0].Notes);
    }
}
