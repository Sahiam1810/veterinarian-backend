using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed record GetNotificationsByAppointmentIdQuery(Guid AppointmentId) : IRequest<IReadOnlyCollection<Notification>>;
