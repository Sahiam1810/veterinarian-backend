using Application.Common.Abstractions;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class CreateNotificationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateNotificationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = new Notification(
            request.UserId,
            request.AppointmentId,
            request.Message,
            request.SentAt,
            request.Status,
            request.Type);

        await unitOfWork.NotificationsRepository.AddAsync(
            notification,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return notification.Id;
    }
}
