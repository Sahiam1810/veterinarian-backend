using Application.Common.Abstractions;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class UpdateNotificationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateNotificationCommand, bool>
{
    public async Task<bool> Handle(
        UpdateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.NotificationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.Update(
            request.UserId,
            request.AppointmentId,
            request.Message,
            request.SentAt,
            request.Status,
            request.Type);

        await unitOfWork.NotificationsRepository.UpdateAsync(
            notification,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
