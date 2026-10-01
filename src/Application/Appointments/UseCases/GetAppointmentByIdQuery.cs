using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetAppointmentByIdQuery(
    Guid Id,
    Guid ActorUserId = default,
    bool EnforceVeterinarianOwnership = false)
    : IRequest<Appointment>;
