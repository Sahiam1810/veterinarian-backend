using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed record DeleteAppointmentStatusHistoryCommand(Guid Id) : IRequest<bool>;
