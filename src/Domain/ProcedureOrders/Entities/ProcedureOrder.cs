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
        Guid appointmentId,
        bool isInHouse,
        string? referredTo,
        string? referralReason,
        IEnumerable<(Guid ProcedureId, string? Notes)>? items = null)
    {
        if (clientPetId == Guid.Empty)
        {
            throw new ArgumentException("El paciente (ClientPetId) es obligatorio.", nameof(clientPetId));
        }

        if (veterinarianId == Guid.Empty)
        {
            throw new ArgumentException("El veterinario es obligatorio.", nameof(veterinarianId));
        }

        if (appointmentId == Guid.Empty)
        {
            throw new ArgumentException("La consulta de origen (AppointmentId) es obligatoria.", nameof(appointmentId));
        }

        var itemList = items?.ToList() ?? new List<(Guid ProcedureId, string? Notes)>();

        MedicalOrderInHouseRules.Validate(isInHouse, referredTo, referralReason, itemList.Count);

        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        AppointmentId = appointmentId;
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

    public Guid AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public bool IsInHouse { get; private set; }
    public string? ReferredTo { get; private set; }
    public string? ReferralReason { get; private set; }

    public string Status { get; private set; } = "Pendiente";
    public string? ResultFileUrl { get; private set; }

    public IReadOnlyCollection<ProcedureOrderItem> Items => _items.AsReadOnly();

    public void Complete(string? resultFileUrl = null)
    {
        if (Status == "Completada")
        {
            throw new InvalidOperationException("La orden de procedimiento ya se encuentra completada.");
        }

        Status = "Completada";
        ResultFileUrl = string.IsNullOrWhiteSpace(resultFileUrl) ? null : resultFileUrl.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
