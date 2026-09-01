using MediatR;

namespace Application.Appointments.UseCases;

public sealed record UpdateAppointmentCommand(
    Guid Id,
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes) : IRequest;
