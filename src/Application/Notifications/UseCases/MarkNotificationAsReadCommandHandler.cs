using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Notifications.UseCases;

// S43: acción angosta (solo marcar como leída), sin depender del permiso
// general "Notificaciones.Edit" — ningún rol lo tiene, ni falta que le haga.
public sealed class MarkNotificationAsReadCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.NotificationsRepository.GetByIdAsync(
            request.NotificationId,
            cancellationToken)
            ?? throw new NotFoundException("Notificación no encontrada.");

        if (notification.UserId != request.ActorPersonId)
        {
            throw new ForbiddenException(
                "No puedes marcar como leída una notificación de otro usuario.");
        }

        notification.MarkAsRead();

        await unitOfWork.NotificationsRepository.UpdateAsync(
            notification,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
