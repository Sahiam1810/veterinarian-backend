using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed record UpdateStatusAppointmentCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest;
