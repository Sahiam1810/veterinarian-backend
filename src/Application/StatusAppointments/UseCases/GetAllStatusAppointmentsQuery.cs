using Domain.StatusAppointments.Entities;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed record GetAllStatusAppointmentsQuery
    : IRequest<IReadOnlyCollection<StatusAppointment>>;
