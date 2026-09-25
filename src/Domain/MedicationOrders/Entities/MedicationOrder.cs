using Domain.Appointments.Entities;
using Domain.ClientsPets.Entities;
using Domain.Common;
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

        var origin = MedicalOrderOriginRules.Normalize(appointmentId, hospitalizationStayId);

        var itemList = items?.ToList() ?? new List<(Guid MedicationId, string? Notes)>();

        MedicalOrderInHouseRules.Validate(isInHouse, referredTo, referralReason, itemList.Count);

        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        AppointmentId = origin.AppointmentId;
        HospitalizationStayId = origin.HospitalizationStayId;
        IsInHouse = isInHouse;
        ReferredTo = isInHouse ? null : referredTo?.Trim();
        ReferralReason = isInHouse ? null : referralReason?.Trim();
        Status = PendingStatus;
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

    public bool IsInHouse { get; private set; }
    public string? ReferredTo { get; private set; }
    public string? ReferralReason { get; private set; }

    public string Status { get; private set; } = PendingStatus;

    public IReadOnlyCollection<MedicationOrderItem> Items => _items.AsReadOnly();

    public const string PendingStatus = "Pendiente";
    public const string DeliveredStatus = "Entregada";
    public const string CancelledStatus = "Cancelada";

    public bool IsPending => string.Equals(Status, PendingStatus, StringComparison.OrdinalIgnoreCase);

    // Mensaje de por qué la orden ya no admite transición (solo Pendiente → Entregada/Cancelada).
    public string DescribeInvalidTransition()
    {
        if (string.Equals(Status, DeliveredStatus, StringComparison.OrdinalIgnoreCase))
        {
            return "La orden de medicamento ya se encuentra entregada.";
        }

        if (string.Equals(Status, CancelledStatus, StringComparison.OrdinalIgnoreCase))
        {
            return "La orden de medicamento está cancelada y no se puede modificar.";
        }

        return $"La orden de medicamento no está pendiente (estado actual: {Status}).";
    }

    public void Cancel()
    {
        if (!IsPending)
        {
            throw new InvalidOperationException(DescribeInvalidTransition());
        }

        Status = CancelledStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (!IsPending)
        {
            throw new InvalidOperationException(DescribeInvalidTransition());
        }

        Status = DeliveredStatus;
        UpdatedAt = DateTime.UtcNow;
    }

}
