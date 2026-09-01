using MediatR;

namespace Application.Notifications.UseCases;

public sealed record DeleteNotificationCommand(Guid Id) : IRequest;
