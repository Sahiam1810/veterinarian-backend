using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed record GetNotificationByIdQuery(Guid Id) : IRequest<Notification?>;
