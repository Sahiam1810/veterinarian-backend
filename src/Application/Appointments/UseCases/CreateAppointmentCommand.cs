using MediatR;

namespace Application.Appointments.UseCases;

public sealed record CreateAppointmentCommand(
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes,
    // Opcional si el cliente ya tiene telefono en perfil (ResolveAsync lo exige al crear).
    string? RequesterPhoneNumber,
    string? ConsultingRoom = null) : IRequest<Guid>;
