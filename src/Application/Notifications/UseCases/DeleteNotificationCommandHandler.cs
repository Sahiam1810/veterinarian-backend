using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class DeleteNotificationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteNotificationCommand>
{
    public async Task Handle(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.NotificationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Notificación no encontrada.");

        await unitOfWork.NotificationsRepository.DeleteAsync(
            notification,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
