using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed record GetNotificationsByUserIdQuery(Guid UserId) : IRequest<IReadOnlyCollection<Notification>>;
