using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetAllAppointmentsQuery
    : IRequest<IReadOnlyCollection<Appointment>>;
