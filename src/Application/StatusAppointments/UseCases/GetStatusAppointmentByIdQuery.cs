using Domain.StatusAppointments.Entities;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed record GetStatusAppointmentByIdQuery(Guid Id)
    : IRequest<StatusAppointment?>;
