using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetAppointmentByIdQuery(Guid Id)
    : IRequest<Appointment?>;
