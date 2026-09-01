using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed record GetAppointmentStatusHistoryByIdQuery(Guid Id)
    : IRequest<AppointmentStatusHistory>;
