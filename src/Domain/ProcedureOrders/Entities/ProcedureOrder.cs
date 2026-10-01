using Domain.Appointments.Entities;
using Domain.ClientsPets.Entities;
using Domain.Common;
using Domain.MedicalOrders;
using Domain.Veterinarians.Entities;

namespace Domain.ProcedureOrders.Entities;

public sealed class ProcedureOrder : BaseEntity<Guid>
{
    private readonly List<ProcedureOrderItem> _items = new();

    private ProcedureOrder()
    {
    }

    public ProcedureOrder(
        Guid clientPetId,
        Guid veterinarianId,
        Guid? appointmentId,
        bool isInHouse,
        string? referredTo,
        string? referralReason,
        IEnumerable<(Guid ProcedureId, string? Notes)>? items = null,
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

        var itemList = items?.ToList() ?? new List<(Guid ProcedureId, string? Notes)>();

        MedicalOrderInHouseRules.Validate(isInHouse, referredTo, referralReason, itemList.Count);

        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        AppointmentId = origin.AppointmentId;
        HospitalizationStayId = origin.HospitalizationStayId;
        IsInHouse = isInHouse;
        ReferredTo = isInHouse ? null : referredTo?.Trim();
        ReferralReason = isInHouse ? null : referralReason?.Trim();
        Status = "Pendiente";
        ResultFileUrl = null;
        CreatedAt = DateTime.UtcNow;

        if (isInHouse)
        {
            foreach (var item in itemList)
            {
                _items.Add(new ProcedureOrderItem(Id, item.ProcedureId, item.Notes));
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
    public string? ResultFileUrl { get; private set; }

    public IReadOnlyCollection<ProcedureOrderItem> Items => _items.AsReadOnly();

    public const string PendingStatus = "Pendiente";
    public const string CompletedStatus = "Completada";
    public const string CancelledStatus = "Cancelada";

    public bool IsPending => string.Equals(Status, PendingStatus, StringComparison.OrdinalIgnoreCase);

    // Mensaje de por qué la orden ya no admite transición (solo Pendiente → Completada/Cancelada).
    public string DescribeInvalidTransition()
    {
        if (string.Equals(Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return "La orden de procedimiento ya se encuentra completada.";
        }

        if (string.Equals(Status, CancelledStatus, StringComparison.OrdinalIgnoreCase))
        {
            return "La orden de procedimiento está cancelada y no se puede modificar.";
        }

        return $"La orden de procedimiento no está pendiente (estado actual: {Status}).";
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

    public void Complete(string? resultFileUrl = null)
    {
        if (!IsPending)
        {
            throw new InvalidOperationException(DescribeInvalidTransition());
        }

        Status = CompletedStatus;
        ResultFileUrl = string.IsNullOrWhiteSpace(resultFileUrl) ? null : resultFileUrl.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
