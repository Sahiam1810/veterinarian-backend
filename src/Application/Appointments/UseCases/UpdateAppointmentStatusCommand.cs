using MediatR;

namespace Application.Appointments.UseCases;

public sealed record UpdateAppointmentStatusCommand(
    Guid AppointmentId,
    Guid StatusId,
    string? Comment,
    Guid ActorUserAccountId = default,
    bool EnforceVeterinarianOwnership = false) : IRequest;
