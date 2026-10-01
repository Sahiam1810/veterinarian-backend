using MediatR;

namespace Application.Appointments.UseCases;

public sealed record QuickBookingAppointmentCommand(
    Guid? ClientId,
    string? ClientPhoneNumber,
    string? ClientFullName,
    string PetName,
    Guid SpeciesId,
    Guid ServiceId,
    Guid VeterinarianId,
    DateTime ScheduledStart,
    DateTime? ScheduledEnd = null,
    Guid? AvailabilityId = null,
    string? ConsultingRoom = null,
    string? Notes = null
) : IRequest<Guid>;
