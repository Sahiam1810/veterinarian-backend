using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed record GetAllNotificationsQuery : IRequest<IReadOnlyCollection<Notification>>;
