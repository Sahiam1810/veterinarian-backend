using Domain.Appointments.Entities;
using Domain.ClientsPets.Entities;
using Domain.Common;
using Domain.HospitalizationStays.Entities;
using Domain.MedicalOrders;
using Domain.Veterinarians.Entities;

namespace Domain.MedicationOrders.Entities;

public sealed class MedicationOrder : BaseEntity<Guid>
{
    private readonly List<MedicationOrderItem> _items = new();

    private MedicationOrder()
    {
    }

    public MedicationOrder(
        Guid clientPetId,
        Guid veterinarianId,
        Guid? appointmentId,
        bool isInHouse,
        string? referredTo,
        string? referralReason,
        IEnumerable<(Guid MedicationId, string? Notes)>? items = null,
        Guid? hospitalizationStayId = null)
    {
        if (clientPetId == Guid.Empty)
        {
            throw new ArgumentException("El paciente (ClientPetId) es obligatorio.", nameof(clientPetId));
        }

        if (veterinarianId == Guid.Empty)
        {
            throw new ArgumentException("El veterinario es obligatorio.", nameof(veterinarianId));
        }

        var hasAppointment = appointmentId.HasValue && appointmentId.Value != Guid.Empty;
        var hasStay = hospitalizationStayId.HasValue && hospitalizationStayId.Value != Guid.Empty;

        if (!hasAppointment && !hasStay)
        {
            throw new ArgumentException("Debe especificar exactamente uno de los orígenes: AppointmentId o HospitalizationStayId.");
        }

        if (hasAppointment && hasStay)
        {
            throw new ArgumentException("La orden no puede estar asociada simultáneamente a una consulta y a una estancia de hospitalización.");
        }

        var itemList = items?.ToList() ?? new List<(Guid MedicationId, string? Notes)>();

        MedicalOrderInHouseRules.Validate(isInHouse, referredTo, referralReason, itemList.Count);

        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        AppointmentId = hasAppointment ? appointmentId : null;
        HospitalizationStayId = hasStay ? hospitalizationStayId : null;
        IsInHouse = isInHouse;
        ReferredTo = isInHouse ? null : referredTo?.Trim();
        ReferralReason = isInHouse ? null : referralReason?.Trim();
        Status = "Pendiente";
        CreatedAt = DateTime.UtcNow;

        if (isInHouse)
        {
            foreach (var item in itemList)
            {
                _items.Add(new MedicationOrderItem(Id, item.MedicationId, item.Notes));
            }
        }
    }

    public Guid ClientPetId { get; private set; }
    public ClientPetEntity? ClientPet { get; private set; }

    public Guid VeterinarianId { get; private set; }
    public Veterinarian? Veterinarian { get; private set; }

    public Guid? AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public Guid? HospitalizationStayId { get; private set; }
    public HospitalizationStay? HospitalizationStay { get; private set; }

    public bool IsInHouse { get; private set; }
    public string? ReferredTo { get; private set; }
    public string? ReferralReason { get; private set; }

    public string Status { get; private set; } = "Pendiente";

    public IReadOnlyCollection<MedicationOrderItem> Items => _items.AsReadOnly();

    public void Complete()
    {
        if (string.Equals(Status, "Entregada", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La orden de medicamento ya se encuentra entregada.");
        }

        if (string.Equals(Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No se puede entregar una orden de medicamento cancelada.");
        }

        if (!string.Equals(Status, "Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Transición de estado no permitida para la orden de medicamento: {Status}.");
        }

        Status = "Entregada";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (string.Equals(Status, "Entregada", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No se puede cancelar una orden de medicamento entregada.");
        }

        if (string.Equals(Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La orden de medicamento ya se encuentra cancelada.");
        }

        Status = "Cancelada";
        UpdatedAt = DateTime.UtcNow;
    }
}

