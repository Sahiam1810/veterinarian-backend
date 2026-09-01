using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class UpdateNotificationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateNotificationCommand>
{
    public async Task Handle(
        UpdateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.NotificationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Notificación no encontrada.");

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
    }
}
