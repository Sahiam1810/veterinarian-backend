using Domain.Appointments.ValueObjects;
using Domain.Availabilities.Entities;
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
        Guid availabilityId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        string? notes,
        string? requesterPhoneNumber = null,
        string? bookingRequestKeyHash = null)
    {
        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        ServiceId = serviceId;
        StatusId = statusId;
        AvailabilityId = availabilityId;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        Notes = notes;
        // Se fija al crear; no se altera en Update (auditoría de origen).
        // Nullable solo para citas legacy anteriores a la columna.
        RequesterPhoneNumber = string.IsNullOrWhiteSpace(requesterPhoneNumber)
            ? null
            : RequesterPhoneNumber.Create(requesterPhoneNumber);
        BookingRequestKeyHash = string.IsNullOrWhiteSpace(bookingRequestKeyHash)
            ? null
            : BookingRequestKeyHash.Create(bookingRequestKeyHash);
    }

    public Guid ClientPetId { get; private set; }
    public ClientPetEntity? ClientPet { get; private set; }

    public Guid VeterinarianId { get; private set; }
    public Veterinarian? Veterinarian { get; private set; }

    public Guid ServiceId { get; private set; }
    public Service? Service { get; private set; }

    public Guid StatusId { get; private set; }
    public StatusAppointment? Status { get; private set; }

    public Guid AvailabilityId { get; private set; }
    public Availability? Availability { get; private set; }

    public DateTime ScheduledStart { get; private set; }
    public DateTime ScheduledEnd { get; private set; }
    public string? Notes { get; private set; }

    public RequesterPhoneNumber? RequesterPhoneNumber { get; private set; }

    public BookingRequestKeyHash? BookingRequestKeyHash { get; private set; }

    public void Update(
        Guid clientPetId,
        Guid veterinarianId,
        Guid serviceId,
        Guid statusId,
        Guid availabilityId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        string? notes)
    {
        ClientPetId = clientPetId;
        VeterinarianId = veterinarianId;
        ServiceId = serviceId;
        StatusId = statusId;
        AvailabilityId = availabilityId;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    // Reagendado por autoservicio del cliente (tras OTP).
    public void Reschedule(
        Guid availabilityId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        string? notes)
    {
        AvailabilityId = availabilityId;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        if (notes is not null)
        {
            Notes = notes;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
