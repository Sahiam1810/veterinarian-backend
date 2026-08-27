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
    string? Notes) : IRequest<Guid>;
