using Application.Common.Abstractions;
using MediatR;

namespace Application.Notifications.UseCases;

public sealed class DeleteNotificationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteNotificationCommand, bool>
{
    public async Task<bool> Handle(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.NotificationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (notification is null)
        {
            return false;
        }

        await unitOfWork.NotificationsRepository.DeleteAsync(
            notification,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
