using MediatR;

namespace Application.Appointments.UseCases;

public sealed record CreateAppointmentCommand(
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    string? Reason,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes) : IRequest<Guid>;
