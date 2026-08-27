using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed record GetAllAppointmentStatusHistoriesQuery
    : IRequest<IReadOnlyCollection<AppointmentStatusHistory>>;
