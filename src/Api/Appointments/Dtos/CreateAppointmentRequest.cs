namespace Api.Appointments.Dtos;

public sealed record CreateAppointmentRequest(
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes,
    // Opcional: si falta, se usa el telefono del dueño de la mascota.
    string? RequesterPhoneNumber = null,
    string? ConsultingRoom = null);
