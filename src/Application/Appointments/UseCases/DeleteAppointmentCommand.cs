using MediatR;

namespace Application.Appointments.UseCases;

public sealed record DeleteAppointmentCommand(Guid Id) : IRequest<bool>;
