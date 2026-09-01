using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed record DeleteStatusAppointmentCommand(Guid Id) : IRequest;
