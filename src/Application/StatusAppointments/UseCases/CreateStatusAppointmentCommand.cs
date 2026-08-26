using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed record CreateStatusAppointmentCommand(
    string Name,
    string? Description) : IRequest<Guid>;
