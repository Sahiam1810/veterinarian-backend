using Domain.ClientsPets.Entities;
using Domain.Common;
using Domain.Services.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Veterinarians.Entities;

namespace Domain.Appointments.Entities;

public sealed class Appointment : BaseEntity<Guid>
{
    private Appointment()
    {
    }

    public Appointment(
        Guid clientPetId,
        Guid veterinarianId,
        Guid serviceId,
        Guid statusId,
        string? reason,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        string? notes)
    {
        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        ServiceId = serviceId;
        StatusId = statusId;
        Reason = reason;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        Notes = notes;
    }

    public Guid ClientPetId { get; private set; }
    public ClientPetEntity? ClientPet { get; private set; }

    public Guid VeterinarianId { get; private set; }
    public Veterinarian? Veterinarian { get; private set; }

    public Guid ServiceId { get; private set; }
    public Service? Service { get; private set; }

    public Guid StatusId { get; private set; }
    public StatusAppointment? Status { get; private set; }

    public string? Reason { get; private set; }
    public DateTime ScheduledStart { get; private set; }
    public DateTime ScheduledEnd { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        Guid clientPetId,
        Guid veterinarianId,
        Guid serviceId,
        Guid statusId,
        string? reason,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        string? notes)
    {
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        ServiceId = serviceId;
        StatusId = statusId;
        Reason = reason;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
