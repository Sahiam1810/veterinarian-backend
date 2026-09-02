using MediatR;

namespace Application.Appointments.UseCases;

public sealed record UpdateAppointmentStatusCommand(
    Guid AppointmentId,
    Guid StatusId,
    string? Comment) : IRequest;
